using System;
using System.Windows.Forms;

using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace LyreUpdater
{
    static class Program
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Disable multiple instances
            bool createdNew = true;
            using (Mutex mutex = new Mutex(true, Assembly.GetExecutingAssembly().GetName().Name, out createdNew))
            {
                if (createdNew)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Console.WriteLine(Directory.GetCurrentDirectory());
                    Console.WriteLine(Environment.NewLine + "Context: " + OnlineResource.context + Environment.NewLine);
                    Console.WriteLine("Updater folder: " + OnlineResource.LyreUpdaterLocation + Environment.NewLine);
                    Console.WriteLine("Downloader folder: " + OnlineResource.LyreDownloaderLocation + Environment.NewLine + Environment.NewLine);
                    Application.Run(new Form1());
                }
                else
                {
                    Process current = Process.GetCurrentProcess();
                    foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                    {
                        if (process.Id != current.Id)
                        {
                            SetForegroundWindow(process.MainWindowHandle);
                            break;
                        }
                    }
                }
            }
        }
    }
}
