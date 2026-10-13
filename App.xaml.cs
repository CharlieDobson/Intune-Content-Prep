using System.Text;
using System.Windows;

namespace IntuneContentPrep
{
    public partial class App : Application
    {
        protected override void OnStartup( StartupEventArgs args )
        {
            // IntuneWinAppUtil.exe writes its console output in the OEM code page
            // (437 on en-US). .NET only ships UTF-8/UTF-16/ASCII/Latin-1 out of the
            // box, so register the legacy code pages to decode it faithfully.
            Encoding.RegisterProvider( CodePagesEncodingProvider.Instance );
            base.OnStartup( args );
        }
    }
}
