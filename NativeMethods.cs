using System.Runtime.InteropServices;

namespace IntuneContentPrep
{
    internal static class NativeMethods
    {
        /// <summary>The system OEM code page - what a new console (and so a redirected
        /// .NET Framework console app like IntuneWinAppUtil.exe) writes in.</summary>
        [DllImport( "kernel32.dll" )]
        public static extern uint GetOEMCP();

        public const uint MONITOR_DEFAULTTONEAREST = 2;

        [DllImport( "user32.dll" )]
        public static extern IntPtr MonitorFromWindow( IntPtr hwnd, uint flags );

        [DllImport( "user32.dll", CharSet = CharSet.Unicode )]
        [return: MarshalAs( UnmanagedType.Bool )]
        public static extern bool GetMonitorInfo( IntPtr monitor, ref MONITORINFO info );

        [StructLayout( LayoutKind.Sequential )]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout( LayoutKind.Sequential )]
        public struct MONITORINFO
        {
            public int Size;
            public RECT Monitor;
            public RECT WorkArea;
            public uint Flags;
        }
    }
}
