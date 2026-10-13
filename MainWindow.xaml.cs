using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace IntuneContentPrep
{
    public partial class MainWindow : Window
    {
        // PSAppDeployToolkit v4 always launches through this exe, and IntuneWinAppUtil
        // names its output after the setup file.
        private const string SetupFileName = "Invoke-AppDeployToolkit.exe";
        private const string PackageFileName = "Invoke-AppDeployToolkit.intunewin";
        private const string ToolFileName = "IntuneWinAppUtil.exe";

        // Height the terminal opens at; the window grows by this much on the first Create.
        private const double TerminalStartHeight = 340;

        private readonly AppSettings _settings = AppSettings.Load();

        // Where IntuneWinAppUtil.exe was found automatically (beside the app, then PATH), if anywhere.
        private (string Path, string Where)? _detectedTool;

        private Process? _process;
        private bool _stopRequested;
        private bool _lastLineBlank;
        private DateTime _runStartUtc;
        private string? _runPackagePath;

        public MainWindow()
        {
            InitializeComponent();
            SourceBox.Text = _settings.SourceFolder ?? "";
            OutputBox.Text = _settings.OutputFolder ?? "";

            // A remembered tool location always wins; auto-detection only fills an empty box.
            _detectedTool = DetectTool();
            ToolBox.Text = !string.IsNullOrWhiteSpace( _settings.ToolPath ) ? _settings.ToolPath : _detectedTool?.Path ?? "";
            UpdateHints();
        }

        private bool IsRunning => _process != null;

        private void Window_Loaded( object sender, RoutedEventArgs args )
        {
            // Nothing below the buttons until the terminal opens, so don't let the
            // window be dragged taller into empty space before then.
            MinHeight = ActualHeight;
            MaxHeight = ActualHeight;
        }

        // ----------------------------------------------------------------------
        // Inputs
        // ----------------------------------------------------------------------
        private void SourceBrowse_Click( object sender, RoutedEventArgs args )
        {
            string? folder = PickFolder( "Select the PSAppDeployToolkit package folder", SourceBox.Text, null );
            if ( folder != null ) SourceBox.Text = folder;
        }

        private void OutputBrowse_Click( object sender, RoutedEventArgs args )
        {
            // With no output chosen yet, start beside the package - the usual place for it.
            string? fallback = NormalizePath( SourceBox.Text ) is string source ? Path.GetDirectoryName( source ) : null;
            string? folder = PickFolder( "Select the output folder for the .intunewin file", OutputBox.Text, fallback );
            if ( folder != null ) OutputBox.Text = folder;
        }

        private void ToolBrowse_Click( object sender, RoutedEventArgs args )
        {
            var dialog = new OpenFileDialog
            {
                Title = $"Locate {ToolFileName}",
                // Wildcard so a renamed copy kept alongside others (e.g. IntuneWinAppUtil-1.8.7.exe) can be picked.
                Filter = "IntuneWinAppUtil (IntuneWinAppUtil*.exe)|IntuneWinAppUtil*.exe|Programs (*.exe)|*.exe",
                CheckFileExists = true,
            };
            string? start = NearestExistingFolder( ToolBox.Text );
            if ( start != null ) dialog.InitialDirectory = start;
            if ( dialog.ShowDialog( this ) == true ) ToolBox.Text = dialog.FileName;
        }

        private string? PickFolder( string title, string current, string? fallback )
        {
            var dialog = new OpenFolderDialog { Title = title };
            string? start = NearestExistingFolder( current ) ?? NearestExistingFolder( fallback );
            if ( start != null ) dialog.InitialDirectory = start;
            return dialog.ShowDialog( this ) == true ? dialog.FolderName : null;
        }

        private static string? NearestExistingFolder( string? text )
        {
            string? folder = NormalizePath( text );
            while ( folder != null && !Directory.Exists( folder ) )
            {
                folder = Path.GetDirectoryName( folder );
            }
            return folder;
        }

        private void Folder_PreviewDragOver( object sender, DragEventArgs args )
        {
            args.Effects = GetDroppedFolder( args ) != null ? DragDropEffects.Copy : DragDropEffects.None;
            args.Handled = true;
        }

        private void Folder_PreviewDrop( object sender, DragEventArgs args )
        {
            string? folder = GetDroppedFolder( args );
            if ( folder != null && sender is TextBox box ) box.Text = folder;
            args.Handled = true;
        }

        private static string? GetDroppedFolder( DragEventArgs args )
        {
            if ( args.Data.GetData( DataFormats.FileDrop ) is not string[] paths || paths.Length != 1 ) return null;
            if ( Directory.Exists( paths[0] ) ) return paths[0];

            // Dropping a file (e.g. Invoke-AppDeployToolkit.exe itself) means the folder it's in.
            return File.Exists( paths[0] ) ? Path.GetDirectoryName( paths[0] ) : null;
        }

        private void Tool_PreviewDragOver( object sender, DragEventArgs args )
        {
            args.Effects = GetDroppedTool( args ) != null ? DragDropEffects.Copy : DragDropEffects.None;
            args.Handled = true;
        }

        private void Tool_PreviewDrop( object sender, DragEventArgs args )
        {
            string? tool = GetDroppedTool( args );
            if ( tool != null ) ToolBox.Text = tool;
            args.Handled = true;
        }

        private static string? GetDroppedTool( DragEventArgs args )
        {
            if ( args.Data.GetData( DataFormats.FileDrop ) is not string[] paths || paths.Length != 1 ) return null;

            // Either the exe itself or the folder it lives in.
            string candidate = Directory.Exists( paths[0] ) ? Path.Combine( paths[0], ToolFileName ) : paths[0];
            return File.Exists( candidate ) && candidate.EndsWith( ".exe", StringComparison.OrdinalIgnoreCase ) ? candidate : null;
        }

        private void Input_TextChanged( object sender, TextChangedEventArgs args ) => UpdateHints();

        private void UpdateHints()
        {
            // TextChanged can fire while InitializeComponent is still building the tree.
            if ( SourceHint == null || OutputHint == null || ToolHint == null ) return;

            string? source = NormalizePath( SourceBox.Text );
            if ( source != null && Directory.Exists( source ) && !File.Exists( Path.Combine( source, SetupFileName ) ) )
            {
                SetHint( SourceHint, $"{SetupFileName} was not found in this folder", "SystemFillColorCriticalBrush" );
            }
            else if ( source != null && Directory.Exists( source ) )
            {
                SetHint( SourceHint, DescribePackage( source ), "TextFillColorSecondaryBrush" );
            }
            else
            {
                SetHint( SourceHint, $"Setup file: {SetupFileName}", "TextFillColorSecondaryBrush" );
            }

            string? output = NormalizePath( OutputBox.Text );
            // Create asks before replacing, so an existing file is just noted here.
            bool exists = output != null && File.Exists( Path.Combine( output, PackageFileName ) );
            SetHint( OutputHint, $"Output file: {PackageFileName}" + ( exists ? "  (already exists here)" : "" ), "TextFillColorSecondaryBrush" );

            string? tool = ResolveToolPath( ToolBox.Text );
            if ( ToolBox.Text.Trim().Length == 0 )
            {
                SetHint( ToolHint, "Not found beside this app or on PATH. Browse to it.", "SystemFillColorCriticalBrush" );
            }
            else if ( tool == null || !File.Exists( tool ) )
            {
                SetHint( ToolHint, "File not found", "SystemFillColorCriticalBrush" );
            }
            else if ( _detectedTool is { } detected && SamePath( tool, detected.Path ) )
            {
                SetHint( ToolHint, detected.Where, "TextFillColorSecondaryBrush" );
            }
            else
            {
                SetHint( ToolHint, "Remembered between launches", "TextFillColorSecondaryBrush" );
            }
        }

        private static void SetHint( TextBlock hint, string text, string brushKey )
        {
            hint.Text = text;
            hint.SetResourceReference( TextBlock.ForegroundProperty, brushKey );
        }

        /// <summary>
        /// One-line summary of a PSADT package folder, e.g.
        /// "Adobe Acrobat Reader 24.1  ·  PSAppDeployToolkit 4.1.8  ·  Setup file: Invoke-AppDeployToolkit.exe",
        /// so it's obvious the right package was picked.
        /// </summary>
        private static string DescribePackage( string source )
        {
            var parts = new List<string>();

            string script = ReadTextOrEmpty( Path.Combine( source, "Invoke-AppDeployToolkit.ps1" ) );
            string app = string.Join( " ", new[] { "AppVendor", "AppName", "AppVersion" }
                .Select( name => MatchQuotedValue( script, name ) )
                .Where( value => value.Length > 0 ) );
            if ( app.Length > 0 ) parts.Add( app );

            string manifest = ReadTextOrEmpty( Path.Combine( source, "PSAppDeployToolkit", "PSAppDeployToolkit.psd1" ) );
            string version = MatchQuotedValue( manifest, "ModuleVersion" );
            if ( version.Length > 0 ) parts.Add( $"PSAppDeployToolkit {version}" );

            parts.Add( $"Setup file: {SetupFileName}" );
            return string.Join( "  ·  ", parts );
        }

        private static string MatchQuotedValue( string text, string name )
        {
            // Matches PowerShell hashtable / manifest entries such as:  AppName = 'Reader'
            Match match = Regex.Match( text, $@"^\s*{name}\s*=\s*['""]([^'""]*)['""]", RegexOptions.Multiline | RegexOptions.IgnoreCase );
            return match.Success ? match.Groups[1].Value.Trim() : "";
        }

        private static string ReadTextOrEmpty( string path )
        {
            try
            {
                return File.Exists( path ) ? File.ReadAllText( path ) : "";
            }
            catch ( Exception )
            {
                return "";
            }
        }

        /// <summary>Full path without a trailing separator, or null if the text isn't a usable absolute path.</summary>
        private static string? NormalizePath( string? text )
        {
            string trimmed = ( text ?? "" ).Trim().Trim( '"' ).Trim();
            if ( trimmed.Length == 0 ) return null;
            try
            {
                string expanded = Environment.ExpandEnvironmentVariables( trimmed );
                if ( !Path.IsPathFullyQualified( expanded ) ) return null;
                return Path.TrimEndingDirectorySeparator( Path.GetFullPath( expanded ) );
            }
            catch ( Exception )
            {
                return null;
            }
        }

        private static bool IsSameOrInside( string path, string folder )
        {
            string folderWithSeparator = folder.EndsWith( Path.DirectorySeparatorChar ) ? folder : folder + Path.DirectorySeparatorChar;
            return string.Equals( path, folder, StringComparison.OrdinalIgnoreCase )
                || path.StartsWith( folderWithSeparator, StringComparison.OrdinalIgnoreCase );
        }

        // ----------------------------------------------------------------------
        // Create
        // ----------------------------------------------------------------------
        private async void Create_Click( object sender, RoutedEventArgs args )
        {
            if ( IsRunning ) return;
            if ( !TryGetInputs( out string source, out string output, out string tool ) ) return;

            // -q makes IntuneWinAppUtil overwrite silently, so ask here instead.
            string packagePath = Path.Combine( output, PackageFileName );
            if ( File.Exists( packagePath ) )
            {
                MessageBoxResult answer = MessageBox.Show( this,
                    $"{PackageFileName} already exists in the output folder:\n\n{packagePath}\n\nReplace it?",
                    Title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No );
                if ( answer != MessageBoxResult.Yes ) return;
            }

            SourceBox.Text = source;
            OutputBox.Text = output;
            ToolBox.Text = tool;
            _settings.SourceFolder = source;
            _settings.OutputFolder = output;
            _settings.ToolPath = tool;
            _settings.Save();

            await RunToolAsync( tool, source, output, packagePath );
        }

        private bool TryGetInputs( out string source, out string output, out string tool )
        {
            source = "";
            output = "";
            tool = "";

            if ( SourceBox.Text.Trim().Length == 0 )
            {
                return Reject( SourceBox, $"Choose a source folder: the PSAppDeployToolkit v4 package folder that contains {SetupFileName}." );
            }
            string? sourceFolder = NormalizePath( SourceBox.Text );
            if ( sourceFolder == null )
            {
                return Reject( SourceBox, $"The source folder isn't a valid full path:\n\n{SourceBox.Text.Trim()}" );
            }
            if ( !Directory.Exists( sourceFolder ) )
            {
                return Reject( SourceBox, $"The source folder doesn't exist:\n\n{sourceFolder}" );
            }
            if ( !File.Exists( Path.Combine( sourceFolder, SetupFileName ) ) )
            {
                return Reject( SourceBox, $"{SetupFileName} wasn't found in the source folder:\n\n{sourceFolder}\n\n"
                    + "Choose the root folder of a PSAppDeployToolkit v4 package." );
            }

            if ( OutputBox.Text.Trim().Length == 0 )
            {
                return Reject( OutputBox, "Choose an output folder for the .intunewin file." );
            }
            string? outputFolder = NormalizePath( OutputBox.Text );
            if ( outputFolder == null )
            {
                return Reject( OutputBox, $"The output folder isn't a valid full path:\n\n{OutputBox.Text.Trim()}" );
            }
            if ( File.Exists( outputFolder ) )
            {
                return Reject( OutputBox, $"The output path is a file, not a folder:\n\n{outputFolder}" );
            }
            if ( IsSameOrInside( outputFolder, sourceFolder ) )
            {
                // IntuneWinAppUtil packages everything under the source folder, so the
                // .intunewin would be swallowed into the next package built from it.
                return Reject( OutputBox, "The output folder can't be the source folder or a folder inside it - "
                    + "the .intunewin file would end up packaged into the next build." );
            }

            if ( ToolBox.Text.Trim().Length == 0 )
            {
                // Cleared, or nothing was found at startup: look again (a copy may have been
                // dropped beside the app since) before asking the user.
                _detectedTool = DetectTool();
                if ( _detectedTool is { } detected ) ToolBox.Text = detected.Path;
            }
            if ( ToolBox.Text.Trim().Length == 0 )
            {
                return Reject( ToolBox, $"Choose {ToolFileName}. It wasn't found beside this app or on PATH." );
            }
            string? toolPath = ResolveToolPath( ToolBox.Text );
            if ( toolPath == null )
            {
                return Reject( ToolBox, $"The {ToolFileName} location isn't a valid full path:\n\n{ToolBox.Text.Trim()}" );
            }
            if ( !File.Exists( toolPath ) )
            {
                return Reject( ToolBox, $"{ToolFileName} wasn't found:\n\n{toolPath}" );
            }

            source = sourceFolder;
            output = outputFolder;
            tool = toolPath;
            return true;
        }

        private bool Reject( TextBox box, string message )
        {
            MessageBox.Show( this, message, Title, MessageBoxButton.OK, MessageBoxImage.Warning );
            box.Focus();
            box.SelectAll();
            return false;
        }

        /// <summary>
        /// Auto-detects IntuneWinAppUtil.exe: beside this app first (the deployed layout), then
        /// PATH. Only used to fill an empty tool box - a location the user chose always wins.
        /// </summary>
        private static (string Path, string Where)? DetectTool()
        {
            string appFolder = Path.GetDirectoryName( Environment.ProcessPath ) ?? AppContext.BaseDirectory;
            string besideApp = Path.Combine( appFolder, ToolFileName );
            if ( File.Exists( besideApp ) ) return ( besideApp, "Found beside this app" );

            string? onPath = FindOnPath( ToolFileName );
            return onPath != null ? ( onPath, "Found on PATH" ) : null;
        }

        /// <summary>The exe the tool box points at: a file path as typed, or a folder that holds IntuneWinAppUtil.exe.</summary>
        private static string? ResolveToolPath( string text )
        {
            string? path = NormalizePath( text );
            return path != null && Directory.Exists( path ) ? Path.Combine( path, ToolFileName ) : path;
        }

        private static bool SamePath( string first, string second )
        {
            return string.Equals( NormalizePath( first ), NormalizePath( second ), StringComparison.OrdinalIgnoreCase );
        }

        private static string? FindOnPath( string fileName )
        {
            string pathVariable = Environment.GetEnvironmentVariable( "PATH" ) ?? "";
            foreach ( string entry in pathVariable.Split( Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries ) )
            {
                try
                {
                    string candidate = Path.Combine( entry.Trim( '"' ), fileName );
                    if ( File.Exists( candidate ) ) return candidate;
                }
                catch ( ArgumentException )
                {
                    // Malformed PATH entry - skip it.
                }
            }
            return null;
        }

        private async Task RunToolAsync( string tool, string source, string output, string packagePath )
        {
            ShowTerminal();
            ClearTerminal();
            SetRunning( true );

            var startInfo = new ProcessStartInfo( tool )
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = ConsoleEncoding(),
                StandardErrorEncoding = ConsoleEncoding(),
                WorkingDirectory = source,
            };
            startInfo.ArgumentList.Add( "-c" );
            startInfo.ArgumentList.Add( source );
            startInfo.ArgumentList.Add( "-s" );
            startInfo.ArgumentList.Add( SetupFileName );
            startInfo.ArgumentList.Add( "-o" );
            startInfo.ArgumentList.Add( output );
            startInfo.ArgumentList.Add( "-q" );

            WriteLine( "> " + string.Join( " ", new[] { tool }.Concat( startInfo.ArgumentList ).Select( QuoteForDisplay ) ), "TerminalCommand" );
            WriteLine( "", "TerminalText" );

            var stopwatch = Stopwatch.StartNew();
            _runStartUtc = DateTime.UtcNow;
            _runPackagePath = packagePath;
            _stopRequested = false;

            try
            {
                using var process = new Process { StartInfo = startInfo };
                process.Start();
                _process = process;

                // -q means there should never be a prompt; if one appears anyway it reads
                // end-of-input and fails rather than hanging invisibly.
                process.StandardInput.Close();

                Task<bool> stdoutTask = PumpAsync( process.StandardOutput, false );
                Task<bool> stderrTask = PumpAsync( process.StandardError, true );
                await process.WaitForExitAsync();
                bool[] reportedErrors = await Task.WhenAll( stdoutTask, stderrTask );
                stopwatch.Stop();

                if ( _stopRequested ) return;
                ReportResult( process.ExitCode, reportedErrors.Contains( true ), packagePath, stopwatch.Elapsed );
            }
            catch ( Exception ex )
            {
                // Typically a failed start (blocked, not a valid exe); shown in the terminal
                // rather than crashing, since this runs from an async void click handler.
                WriteLine( $"Couldn't run {tool}: {ex.Message}", "TerminalError" );
                SetStatus( "Failed", "TerminalError" );
            }
            finally
            {
                _process = null;
                SetRunning( false );
            }
        }

        private async Task<bool> PumpAsync( StreamReader reader, bool isStandardError )
        {
            bool sawError = false;
            string? line;
            while ( ( line = await reader.ReadLineAsync() ) != null )
            {
                // IntuneWinAppUtil prefixes every line with its level: INFO / WARN / ERROR.
                bool isError = line.StartsWith( "ERROR", StringComparison.OrdinalIgnoreCase )
                    || ( isStandardError && line.Trim().Length > 0 );
                bool isWarning = line.StartsWith( "WARN", StringComparison.OrdinalIgnoreCase );
                sawError |= isError;
                WriteLine( line, isError ? "TerminalError" : isWarning ? "TerminalWarning" : "TerminalText" );
            }
            return sawError;
        }

        private void ReportResult( int exitCode, bool reportedError, string packagePath, TimeSpan elapsed )
        {
            // IntuneWinAppUtil exits 0 even when it prints ERROR, so success also requires
            // that it actually wrote the package during this run.
            var package = new FileInfo( packagePath );
            bool written = package.Exists && package.LastWriteTimeUtc >= _runStartUtc.AddSeconds( -2 );
            string seconds = $"{elapsed.TotalSeconds:0.0} s";

            WriteLine( "", "TerminalText" );
            if ( exitCode == 0 && !reportedError && written )
            {
                var link = new Hyperlink( new Run( packagePath ) ) { ToolTip = "Show in File Explorer", Cursor = Cursors.Hand };
                link.Foreground = (Brush)FindResource( "TerminalLink" );
                link.Click += ( _, _ ) => RevealInExplorer( packagePath );

                var paragraph = NewLine();
                paragraph.Inlines.Add( new Run( "Package created: " ) { Foreground = (Brush)FindResource( "TerminalSuccess" ) } );
                paragraph.Inlines.Add( link );
                paragraph.Inlines.Add( new Run( $"  ({FormatSize( package.Length )}, {seconds})" ) { Foreground = (Brush)FindResource( "TerminalDim" ) } );
                AddLine( paragraph );
                SetStatus( $"Completed in {seconds}", "TerminalSuccess" );
            }
            else
            {
                string reason = reportedError ? "IntuneWinAppUtil.exe reported an error (see above)."
                    : exitCode != 0 ? $"IntuneWinAppUtil.exe exited with code {exitCode}."
                    : $"IntuneWinAppUtil.exe finished without writing {PackageFileName}.";
                WriteLine( $"No package was created. {reason}", "TerminalError" );
                SetStatus( "Failed", "TerminalError" );
            }
        }

        private void SetRunning( bool running )
        {
            SourceBox.IsEnabled = !running;
            OutputBox.IsEnabled = !running;
            ToolBox.IsEnabled = !running;
            SourceBrowseButton.IsEnabled = !running;
            OutputBrowseButton.IsEnabled = !running;
            ToolBrowseButton.IsEnabled = !running;
            CreateButton.IsEnabled = !running;
            RunProgress.Visibility = running ? Visibility.Visible : Visibility.Hidden;
            if ( running ) SetStatus( "Running…", "TerminalDim" );
            if ( !running ) UpdateHints();
        }

        private static Encoding ConsoleEncoding()
        {
            try
            {
                return Encoding.GetEncoding( (int)NativeMethods.GetOEMCP() );
            }
            catch ( Exception )
            {
                return Encoding.Default;
            }
        }

        private static string QuoteForDisplay( string argument )
        {
            return argument.Contains( ' ' ) ? $"\"{argument}\"" : argument;
        }

        private static string FormatSize( long bytes )
        {
            string[] units = { "bytes", "KB", "MB", "GB" };
            double size = bytes;
            int unit = 0;
            while ( size >= 1024 && unit < units.Length - 1 )
            {
                size /= 1024;
                unit++;
            }
            return unit == 0 ? $"{bytes} bytes" : $"{size:0.0} {units[unit]}";
        }

        private static void RevealInExplorer( string path )
        {
            try
            {
                Process.Start( "explorer.exe", $"/select,\"{path}\"" );
            }
            catch ( Exception )
            {
                // Nothing useful to do if Explorer won't start.
            }
        }

        // ----------------------------------------------------------------------
        // Terminal
        // ----------------------------------------------------------------------
        private void ShowTerminal()
        {
            if ( TerminalPanel.Visibility == Visibility.Visible ) return;

            // Give the panel a fixed height, let WPF size the window around it, then
            // hand sizing back to the user with the panel filling whatever they choose.
            MaxHeight = double.PositiveInfinity;
            TerminalPanel.Height = TerminalStartHeight;
            TerminalPanel.Visibility = Visibility.Visible;
            SizeToContent = SizeToContent.Height;
            UpdateLayout();

            SizeToContent = SizeToContent.Manual;
            MinHeight = ActualHeight - TerminalStartHeight + 140;
            TerminalPanel.Height = double.NaN;
            KeepOnScreen();
        }

        /// <summary>Nudges the window up if growing it pushed the bottom off the work area.</summary>
        private void KeepOnScreen()
        {
            IntPtr hwnd = new WindowInteropHelper( this ).Handle;
            var info = new NativeMethods.MONITORINFO { Size = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
            IntPtr monitor = NativeMethods.MonitorFromWindow( hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST );
            if ( !NativeMethods.GetMonitorInfo( monitor, ref info ) ) return;
            if ( PresentationSource.FromVisual( this )?.CompositionTarget is not CompositionTarget target ) return;

            Point workTop = target.TransformFromDevice.Transform( new Point( info.WorkArea.Left, info.WorkArea.Top ) );
            Point workBottom = target.TransformFromDevice.Transform( new Point( info.WorkArea.Right, info.WorkArea.Bottom ) );
            double workHeight = workBottom.Y - workTop.Y;

            if ( ActualHeight > workHeight ) Height = Math.Max( MinHeight, workHeight );
            if ( Top + ActualHeight > workBottom.Y ) Top = Math.Max( workTop.Y, workBottom.Y - ActualHeight );
        }

        private void SetStatus( string text, string brushKey )
        {
            TerminalStatus.Text = text;
            TerminalStatus.Foreground = (Brush)FindResource( brushKey );
        }

        private void ClearTerminal()
        {
            TerminalDocument.Blocks.Clear();
            _lastLineBlank = true;
        }

        private void WriteLine( string text, string brushKey )
        {
            // IntuneWinAppUtil pads its output with runs of blank lines; one is enough on screen.
            bool blank = text.Trim().Length == 0;
            if ( blank && _lastLineBlank ) return;

            var paragraph = NewLine();
            paragraph.Inlines.Add( new Run( text ) );
            paragraph.Foreground = (Brush)FindResource( brushKey );
            AddLine( paragraph );
            _lastLineBlank = blank;
        }

        private static Paragraph NewLine() => new Paragraph { Margin = new Thickness( 0 ) };

        private void AddLine( Paragraph paragraph )
        {
            TerminalDocument.Blocks.Add( paragraph );
            Terminal.ScrollToEnd();
            _lastLineBlank = false;
        }

        // ----------------------------------------------------------------------
        // Exit
        // ----------------------------------------------------------------------
        private void Exit_Click( object sender, RoutedEventArgs args ) => Close();

        protected override void OnClosing( CancelEventArgs args )
        {
            if ( IsRunning )
            {
                MessageBoxResult answer = MessageBox.Show( this,
                    $"{ToolFileName} is still creating the package.\n\nStop it and exit?",
                    Title, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No );
                if ( answer != MessageBoxResult.Yes )
                {
                    args.Cancel = true;
                    return;
                }
                StopTool();
            }

            // Remember the inputs for next time even if Create was never clicked. A cleared
            // tool box is stored empty, which means "auto-detect again" on the next launch.
            _settings.SourceFolder = SourceBox.Text.Trim();
            _settings.OutputFolder = OutputBox.Text.Trim();
            _settings.ToolPath = ToolBox.Text.Trim();
            _settings.Save();
            base.OnClosing( args );
        }

        private void StopTool()
        {
            Process? process = _process;
            if ( process == null || process.HasExited ) return;

            _stopRequested = true;
            try
            {
                process.Kill( entireProcessTree: true );
                process.WaitForExit( 5000 );
            }
            catch ( Exception )
            {
                // It finished on its own in the meantime.
            }

            // A run killed mid-write leaves a truncated package; don't let it pass for a real one.
            if ( _runPackagePath == null ) return;
            try
            {
                var package = new FileInfo( _runPackagePath );
                if ( package.Exists && package.LastWriteTimeUtc >= _runStartUtc.AddSeconds( -2 ) ) package.Delete();
            }
            catch ( Exception )
            {
                // Best effort only.
            }
        }
    }
}
