using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Threading;

namespace CyberPanacea_V2._0
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Check if the application is running as administrator
            if (!IsRunningAsAdmin())
            {
                // Restart the app with elevated rights if not running as admin
                RunAsAdmin();
                return; // Don't continue with the splash screen if restarting as admin
            }

            // If the app is running as admin, show the splash screen
            loader splashScreen = new loader();
            splashScreen.Show();
        }

        // Method to check if the program is running as Administrator
        private bool IsRunningAsAdmin()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        // Method to restart the application as Administrator
        private void RunAsAdmin()
        {
            // Get the directory of the current application
            string appDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            // Get the path to the executable (.exe) instead of the .dll
            string exePath = System.IO.Path.Combine(appDirectory, "CyberPanacea-V2.0.exe");

            // Check if the .exe file exists before trying to start it
            if (!System.IO.File.Exists(exePath))
            {
                MessageBox.Show("Executable file not found. Please ensure the application is built correctly.");
                return;
            }

            // Start the new process with elevated rights
            ProcessStartInfo startInfo = new ProcessStartInfo(exePath)
            {
                Verb = "runas", // This ensures the program is run as Administrator
                UseShellExecute = true
            };

            // Start the process and shutdown the current application
            Process.Start(startInfo);
            Application.Current.Shutdown(); // Exit the current process
        }

    }
}
