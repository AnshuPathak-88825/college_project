using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CyberPanacea_V2._0
{
    public partial class table : Window
    {
        // Model class for file records
        public class FileRecord
        {
            public string FileName { get; set; }
            public string OriginalPath { get; set; }
            public string ThreatType { get; set; }
            public DateTime QuarantineDate { get; set; }
            public DateTime RemovalDate { get; set; }
            public string FileSize { get; set; }
            public string RemovalMethod { get; set; }
        }

        // Lists to store file records
        private List<FileRecord> quarantinedFiles;
        private List<FileRecord> removedFiles;

        public table()
        {
            InitializeComponent();
            InitializeLists();
            LoadSampleData(); // For testing, replace with actual data loading
        }

        private void InitializeLists()
        {
            quarantinedFiles = new List<FileRecord>();
            removedFiles = new List<FileRecord>();
        }

        // Sample data loading method - replace with your actual data source
        private void LoadSampleData()
        {
            // Sample quarantined files
            quarantinedFiles.Add(new FileRecord
            {
                FileName = "test.exe",
                OriginalPath = @"C:\Users\Desktop\test.exe",
                ThreatType = "Malware",
                QuarantineDate = DateTime.Now,
                FileSize = "1.2 MB"
            });

            // Sample removed files
            removedFiles.Add(new FileRecord
            {
                FileName = "suspicious.dll",
                OriginalPath = @"C:\Windows\System32\suspicious.dll",
                ThreatType = "Trojan",
                RemovalDate = DateTime.Now,
                FileSize = "800 KB",
                RemovalMethod = "Deleted"
            });

            // Set the DataGrid ItemsSource
            QuarantineGrid.ItemsSource = quarantinedFiles;
            RemovedGrid.ItemsSource = removedFiles;
        }

        // Method to add new quarantined file
        public void AddQuarantinedFile(FileRecord file)
        {
            quarantinedFiles.Add(file);
            QuarantineGrid.Items.Refresh();
        }

        // Method to add new removed file
        public void AddRemovedFile(FileRecord file)
        {
            removedFiles.Add(file);
            RemovedGrid.Items.Refresh();
        }

        // Event handler for Restore button
        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var file = button.DataContext as FileRecord;
            if (file != null)
            {
                // Add your restore logic here
                quarantinedFiles.Remove(file);
                QuarantineGrid.Items.Refresh();
                MessageBox.Show($"File {file.FileName} restored successfully!");
            }
        }

        // Event handler for Delete button
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var file = button.DataContext as FileRecord;
            if (file != null)
            {
                // Add your delete logic here
                quarantinedFiles.Remove(file);
                QuarantineGrid.Items.Refresh();
                MessageBox.Show($"File {file.FileName} deleted successfully!");
            }
        }

        // Search functionality
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchBox = sender as TextBox;
            var searchText = searchBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                QuarantineGrid.ItemsSource = quarantinedFiles;
                RemovedGrid.ItemsSource = removedFiles;
            }
            else
            {
                QuarantineGrid.ItemsSource = quarantinedFiles.Where(f =>
                    f.FileName.ToLower().Contains(searchText) ||
                    f.OriginalPath.ToLower().Contains(searchText));

                RemovedGrid.ItemsSource = removedFiles.Where(f =>
                    f.FileName.ToLower().Contains(searchText) ||
                    f.OriginalPath.ToLower().Contains(searchText));
            }
        }

        // Export functionality for removed files
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            // Add your export logic here
            MessageBox.Show("Export functionality to be implemented");
        }
    }
}