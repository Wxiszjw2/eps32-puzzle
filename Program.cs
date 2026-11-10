using System;
using System.Reflection;
using System.Windows;

[assembly: AssemblyTitle("XIO Studio Builder + AI Configuration")]
[assembly: AssemblyDescription("Offline profile editor and AI default-configuration generator")]
[assembly: AssemblyVersion("0.1.0.0")]
[assembly: AssemblyFileVersion("0.1.0.0")]

namespace XioPuzzleStudio
{
    public static class Program
    {
        [STAThread]
        public static int Main()
        {
            var app = new Application();
            return app.Run(new MainWindow());
        }
    }
}
