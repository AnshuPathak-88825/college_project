using System;
using System.Threading.Tasks;
using System.Windows;

namespace PacketProtection._0
{
    /// <summary>
    /// Interaction logic for loader.xaml
    /// </summary>
    public partial class loader : Window
    {
        public loader()
        {
            InitializeComponent();
            StartMainWindowAfterDelay();
        }

        private async void StartMainWindowAfterDelay()
        {
            //MessageBox.Show("this is hide");
            // Delay for 10 seconds
            await Task.Delay(TimeSpan.FromSeconds(10));

            // Create an instance of MainWindow
            MainWindow mainWindow = new MainWindow();

            // Show the MainWindow
            mainWindow.Show();

            // Close the loader window
            this.Close();
        }
    }
}
