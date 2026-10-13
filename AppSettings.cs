using System.IO;
using System.Text.Json;

namespace IntuneContentPrep
{
    /// <summary>
    /// Per-user settings, kept in %LOCALAPPDATA%\IntuneContentPrep\settings.json.
    /// A missing or unreadable file just means defaults.
    /// </summary>
    internal sealed class AppSettings
    {
        public string? ToolPath { get; set; }
        public string? SourceFolder { get; set; }
        public string? OutputFolder { get; set; }

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ),
            "IntuneContentPrep", "settings.json" );

        public static AppSettings Load()
        {
            try
            {
                if ( File.Exists( FilePath ) )
                {
                    return JsonSerializer.Deserialize<AppSettings>( File.ReadAllText( FilePath ) ) ?? new AppSettings();
                }
            }
            catch ( Exception )
            {
                // Corrupt or unreadable settings are not worth failing startup over.
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory( Path.GetDirectoryName( FilePath )! );
                File.WriteAllText( FilePath, JsonSerializer.Serialize( this, JsonOptions ) );
            }
            catch ( Exception )
            {
                // Losing remembered folders is harmless; don't interrupt the user for it.
            }
        }
    }
}
