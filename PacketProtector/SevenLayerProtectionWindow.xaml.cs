using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Linq;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Text.Json;
using System.Collections.Specialized;
using Path = System.IO.Path;
using RealTimeProtection;
using System.Threading;


namespace RansomwareProtector
{


    public class ProtectedFolder
    {
        public string Path { get; set; }
        public DateTime DateAdded { get; set; }
        public bool IsPremium { get; set; }
        public bool IsActive { get; set; }

        public ProtectedFolder(string path, bool isPremium = false)
        {
            Path = path;
            DateAdded = DateTime.Now;
            IsPremium = isPremium;
            IsActive = true;
        }
    }



    public class MonitoringDataManager
    {
        private readonly string appDataPath = string.Empty;
        private readonly string jsonFilePath = string.Empty;

        public MonitoringDataManager()
        {
            try
            {
                // Get the application's base directory
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // Initialize appDataPath
                appDataPath = Path.Combine(baseDir, "App_Data");
                Directory.CreateDirectory(appDataPath);

                // Initialize jsonFilePath
                jsonFilePath = Path.Combine(appDataPath, "monitoring.json");
                //MessageBox.Show(jsonFilePath);

                // Create the JSON file if it doesn't exist
                if (!File.Exists(jsonFilePath))
                {
                    var initialData = new MonitoringData
                    {
                        MonitoringFolders = new List<ProtectedFolder>(),
                        PremiumFolders = new List<ProtectedFolder>()
                    };
                    string jsonString = JsonSerializer.Serialize(initialData, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    File.WriteAllText(jsonFilePath, jsonString);
                    Console.WriteLine($"Created new monitoring file at: {jsonFilePath}");
                    //MessageBox.Show(jsonFilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing data manager: {ex.Message}\nPath: {appDataPath}",
                              "Initialization Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                // Initialize with default values if there's an error
                appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
                jsonFilePath = Path.Combine(appDataPath, "monitoring.json");
            }
        }

        public void SaveMonitoringData(ObservableCollection<ProtectedFolder> monitoringFolders,
                                     ObservableCollection<ProtectedFolder> premiumFolders)
        {
            if (string.IsNullOrEmpty(appDataPath) || string.IsNullOrEmpty(jsonFilePath))
            {
                MessageBox.Show("Error: Data paths not properly initialized.",
                              "Save Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                return;
            }

            try
            {
                Directory.CreateDirectory(appDataPath);

                var data = new MonitoringData
                {
                    MonitoringFolders = new List<ProtectedFolder>(monitoringFolders),
                    PremiumFolders = new List<ProtectedFolder>(premiumFolders)
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string jsonString = JsonSerializer.Serialize(data, options);
                File.WriteAllText(jsonFilePath, jsonString);
                Console.WriteLine($"Data saved successfully to: {jsonFilePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving monitoring data: {ex.Message}\nPath: {jsonFilePath}",
                              "Save Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        public MonitoringData LoadMonitoringData()
        {
            if (string.IsNullOrEmpty(jsonFilePath))
            {
                MessageBox.Show("Error: JSON file path not properly initialized.",
                              "Load Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                return new MonitoringData();
            }

            try
            {
                if (File.Exists(jsonFilePath))
                {
                    string jsonString = File.ReadAllText(jsonFilePath);
                    var data = JsonSerializer.Deserialize<MonitoringData>(jsonString);
                    return data ?? new MonitoringData();
                }
                else
                {
                    var newData = new MonitoringData();
                    SaveMonitoringData(
                        new ObservableCollection<ProtectedFolder>(),
                        new ObservableCollection<ProtectedFolder>()
                    );
                    return newData;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading monitoring data: {ex.Message}\nPath: {jsonFilePath}",
                              "Load Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                return new MonitoringData();
            }
        }

        public class MonitoringData
        {
            public List<ProtectedFolder> MonitoringFolders { get; set; } = new List<ProtectedFolder>();
            public List<ProtectedFolder> PremiumFolders { get; set; } = new List<ProtectedFolder>();
        }
    }




    public partial class SevenLayerProtectionWindow : Window
    {
        private MonitoringDataManager dataManager;
        private FolderMonitor monitor;

        public ObservableCollection<ProtectedFolder> MonitoringFolders { get; set; }
        public ObservableCollection<ProtectedFolder> PremiumFolders { get; set; }

        public SevenLayerProtectionWindow()
        {
            InitializeComponent();
            Topmost = true;

            // Initialize data manager immediately
            dataManager = new MonitoringDataManager();
            Console.WriteLine("Data manager initialized"); // Debug output

            // Load saved data
            var savedData = dataManager.LoadMonitoringData();
            Console.WriteLine("Data loaded"); // Debug output

            // Initialize collections with saved data
            MonitoringFolders = new ObservableCollection<ProtectedFolder>(savedData.MonitoringFolders);
            PremiumFolders = new ObservableCollection<ProtectedFolder>(savedData.PremiumFolders);

            MonitoringFoldersList.ItemsSource = MonitoringFolders;
            PremiumFoldersList.ItemsSource = PremiumFolders;

            // Add event handlers for collection changes
            MonitoringFolders.CollectionChanged += Folders_CollectionChanged;
            PremiumFolders.CollectionChanged += Folders_CollectionChanged;
        }

        private void Folders_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                // Save data whenever collections change
                dataManager.SaveMonitoringData(MonitoringFolders, PremiumFolders);
                Console.WriteLine("Folders updated and saved successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving changes: {ex.Message}", "Save Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Error in Folders_CollectionChanged: {ex.Message}");
            }
        }



        private void AddMonitoringFolders_Click(object sender, RoutedEventArgs e)
        {
            // Temporarily disable Topmost before showing dialog
            Topmost = false;

            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Multiselect = true,
                Title = "Select Folders to Monitor"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                foreach (string path in dialog.FileNames)
                {
                    if (!MonitoringFolders.Any(f => f.Path == path))
                    {
                        MonitoringFolders.Add(new ProtectedFolder(path));
                        monitor.reload();
                    }
                }
            }

            // Re-enable Topmost after dialog closes
            Topmost = true;
        }



        private void AddPremiumFolders_Click(object sender, RoutedEventArgs e)
        {
            // Temporarily disable Topmost before showing dialog
            Topmost = false;

            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Multiselect = true,
                Title = "Select Premium Folders"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                foreach (string path in dialog.FileNames)
                {
                    if (!PremiumFolders.Any(f => f.Path == path))
                    {
                        PremiumFolders.Add(new ProtectedFolder(path, true));
                    }
                }
            }

            // Re-enable Topmost after dialog closes
            Topmost = true;
        }


        private void AddMonitoringDrive_Click(object sender, RoutedEventArgs e)
        {
            // Convert DriveInfo objects to a list of drive names (strings)
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => d.Name)
                .ToList();

            var driveSelection = new DriveSelectionWindow(drives, "Select Drive to Monitor")
            {
                Owner = this
            };

            if (driveSelection.ShowDialog() == true && driveSelection.SelectedDrive != null)
            {
                MonitoringFolders.Add(new ProtectedFolder(driveSelection.SelectedDrive));
            }
        }



        private void AddPremiumDrive_Click(object sender, RoutedEventArgs e)
        {
            // Convert DriveInfo objects to a list of drive names (strings)
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => d.Name)
                .ToList();

            var driveSelection = new DriveSelectionWindow(drives, "Select Premium Drive")
            {
                Owner = this
            };

            if (driveSelection.ShowDialog() == true && driveSelection.SelectedDrive != null)
            {
                PremiumFolders.Add(new ProtectedFolder(driveSelection.SelectedDrive, true));
            }
        }


        private void RemoveMonitoringFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string path)
            {
                var folderToRemove = MonitoringFolders.FirstOrDefault(f => f.Path == path);
                if (folderToRemove != null)
                {
                    MonitoringFolders.Remove(folderToRemove);
                        monitor.reload();


                }
            }
        }

        private void RemovePremiumFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string path)
            {
                var folderToRemove = PremiumFolders.FirstOrDefault(f => f.Path == path);
                if (folderToRemove != null)
                {
                    PremiumFolders.Remove(folderToRemove);
                }
            }
        }
    }





    public class DriveSelectionWindow : Window
    {
        public string SelectedDrive { get; private set; }

        public DriveSelectionWindow(List<string> driveNames, string title)
        {
            Title = title;
            Width = 600;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(28, 28, 36));
            ResizeMode = ResizeMode.NoResize;

            // Main container
            var mainGrid = new Grid { Margin = new Thickness(25) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
            var headerText = new TextBlock
            {
                Text = "Select Drive",
                Foreground = Brushes.White,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var subHeaderText = new TextBlock
            {
                Text = "Choose a drive to protect",
                Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            };

            headerPanel.Children.Add(headerText);
            headerPanel.Children.Add(subHeaderText);
            Grid.SetRow(headerPanel, 0);
            mainGrid.Children.Add(headerPanel);

            // Drives container
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            Grid.SetRow(scrollViewer, 1);

            var drivesPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 20)
            };

            foreach (var driveName in driveNames)
            {
                var driveInfo = new DriveInfo(driveName);
                var driveCard = CreateDriveCard(driveInfo);
                driveCard.MouseDown += (s, e) => {
                    foreach (Border card in drivesPanel.Children)
                    {
                        card.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                    }
                    ((Border)s).Background = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                    SelectedDrive = driveName;
                };
                drivesPanel.Children.Add(driveCard);
            }

            scrollViewer.Content = drivesPanel;
            mainGrid.Children.Add(scrollViewer);

            // Buttons panel
            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            Grid.SetRow(buttonsPanel, 2);

            var selectButton = CreateButton("Protect Drive", "#155777", true);
            var cancelButton = CreateButton("Cancel", "#444444", false);

            selectButton.Click += (s, e) =>
            {
                if (SelectedDrive != null)
                {
                    DialogResult = true;
                    Close();
                }
            };

            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };

            buttonsPanel.Children.Add(selectButton);
            buttonsPanel.Children.Add(cancelButton);
            mainGrid.Children.Add(buttonsPanel);

            Content = mainGrid;
        }

        private Border CreateDriveCard(DriveInfo driveInfo)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 10, 10),
                Width = 260,
                Cursor = Cursors.Hand
            };

            // Add hover effect
            card.MouseEnter += (s, e) => {
                if (SelectedDrive != driveInfo.Name)
                {
                    ((Border)s).Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                }
            };
            card.MouseLeave += (s, e) => {
                if (SelectedDrive != driveInfo.Name)
                {
                    ((Border)s).Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                }
            };

            var content = new Grid { Margin = new Thickness(15) };
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Drive icon and name
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };

            var driveIcon = new System.Windows.Shapes.Path
            {
                Data = System.Windows.Media.Geometry.Parse("M4,8 L8,8 L8,4 L12,4 L12,8 L16,8 L16,12 L12,12 L12,16 L8,16 L8,12 L4,12 Z"),
                Fill = System.Windows.Media.Brushes.White,
                Width = 24,
                Height = 24,
                Stretch = System.Windows.Media.Stretch.Uniform,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var driveName = new TextBlock
            {
                Text = driveInfo.Name,
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };

            headerPanel.Children.Add(driveIcon);
            headerPanel.Children.Add(driveName);
            Grid.SetRow(headerPanel, 0);
            content.Children.Add(headerPanel);

            // Drive type
            var driveType = new TextBlock
            {
                Text = driveInfo.DriveType.ToString(),
                Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(driveType, 1);
            content.Children.Add(driveType);

            // Space usage
            if (driveInfo.IsReady)
            {
                var spacePanel = new StackPanel { Margin = new Thickness(0, 5, 0, 0) };

                var progressBar = new ProgressBar
                {
                    Height = 4,
                    Minimum = 0,
                    Maximum = 100,
                    Value = ((double)(driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / driveInfo.TotalSize) * 100,
                    Margin = new Thickness(0, 5, 0, 5)
                };

                var spaceText = new TextBlock
                {
                    Text = $"{FormatSize(driveInfo.AvailableFreeSpace)} free of {FormatSize(driveInfo.TotalSize)}",
                    Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                    FontSize = 12
                };

                spacePanel.Children.Add(progressBar);
                spacePanel.Children.Add(spaceText);
                Grid.SetRow(spacePanel, 2);
                content.Children.Add(spacePanel);
            }

            card.Child = content;
            return card;
        }

        private Button CreateButton(string content, string backgroundColor, bool isPrimary)
        {
            var button = new Button
            {
                Content = content,
                Width = 150,
                Height = 40,
                Margin = new Thickness(5),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroundColor)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                Cursor = Cursors.Hand
            };

            var buttonStyle = new Style(typeof(Button));

            var normalTemplate = new ControlTemplate(typeof(Button));
            var templateContent = new FrameworkElementFactory(typeof(Border));
            templateContent.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            templateContent.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            templateContent.AppendChild(contentPresenter);

            normalTemplate.VisualTree = templateContent;
            buttonStyle.Setters.Add(new Setter(Button.TemplateProperty, normalTemplate));

            // Hover trigger
            var hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            var darkerColor = isPrimary ? "#1A6B94" : "#555555";
            hoverTrigger.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(darkerColor))));
            buttonStyle.Triggers.Add(hoverTrigger);

            button.Style = buttonStyle;
            return button;
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }



}