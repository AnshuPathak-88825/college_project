using CyberPanacea_V2._0.Models;
using CyberPanacea_V2._0.Services;
using RealTimeProtection;
using RansomwareProtector;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;

namespace CyberPanacea_V2._0
{


    public class NotificationManager
    {
        private System.Windows.Forms.NotifyIcon _notifyIcon;

        //private NotifyIcon _notifyIcon;

        public NotificationManager()
        {
            _notifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = SystemIcons.Information,
                BalloonTipIcon = ToolTipIcon.Info
            };
        }

        public void ShowNotification(string title, string message)
        {
            try
            {
                _notifyIcon.BalloonTipTitle = title;
                _notifyIcon.BalloonTipText = message;
                _notifyIcon.ShowBalloonTip(3000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to show notification: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _notifyIcon.Dispose();
        }
    }


    public partial class MainWindow : Window
    {

        private CancellationTokenSource _scanningCts;
        //private FolderMonitor _scanner;
        private readonly NotificationManager _notificationManager = new NotificationManager();
        private SecurityConfig _config;

        private CheckProtection _status_protection;
        private FolderMonitor _folderMonitor;

        private readonly string _configPath;

        private ObservableCollection<string> _monitoredPorts;
        private ObservableCollection<string> _emailRecipients;
        private ObservableCollection<string> _whitelistedIPs;
        private ObservableCollection<string> _blacklistedIPs;

        // Properties for HomePage data binding
        private ObservableCollection<string> _protectionFeaturesList;
        private ObservableCollection<string> _ransomwareFactsList;
        private ObservableCollection<ProtectionLayer> _protectionLayersList;

        public ObservableCollection<string> ProtectionFeaturesList
        {
            get { return _protectionFeaturesList; }
            set { _protectionFeaturesList = value; }
        }

        public ObservableCollection<string> RansomwareFactsList
        {
            get { return _ransomwareFactsList; }
            set { _ransomwareFactsList = value; }
        }

        public ObservableCollection<ProtectionLayer> ProtectionLayersList
        {
            
            get { return _protectionLayersList; }
            set { _protectionLayersList = value; }


        }

        public MainWindow()
        {
            InitializeComponent();
            
            _config = new SecurityConfig();
            _status_protection = new CheckProtection();

            // Initialize collections
            _monitoredPorts = new ObservableCollection<string>();
            _emailRecipients = new ObservableCollection<string>();
            _whitelistedIPs = new ObservableCollection<string>();
            _blacklistedIPs = new ObservableCollection<string>();


        // Initialize ListBox ItemsSource
        PortList.ItemsSource = _monitoredPorts;
            RecipientList.ItemsSource = _emailRecipients;
            WhitelistIPList.ItemsSource = _whitelistedIPs;
            BlacklistIPList.ItemsSource = _blacklistedIPs;
            _status_protection.RefreshSystemInfo();
            
            // Initialize HomePage data
            InitializeHomePageData();
            DataContext = this;


            // Set config path to current directory
            var _configPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "monitoring.json");

            // Initialize collections
            InitializeCollections();

            // Load or create configuration
            LoadOrCreateConfig();

            // Initialize UI bindings
            InitializeUIBindings();

            // Load configuration into UI
            LoadSecurityConfig();
        }

        private void InitializeCollections()
        {
            _monitoredPorts = new ObservableCollection<string>();
            _emailRecipients = new ObservableCollection<string>();
            _whitelistedIPs = new ObservableCollection<string>();
            _blacklistedIPs = new ObservableCollection<string>();
        }

        private void InitializeUIBindings()
        {
            // Bind collections to ListBoxes
            PortList.ItemsSource = _monitoredPorts;
            RecipientList.ItemsSource = _emailRecipients;
            WhitelistIPList.ItemsSource = _whitelistedIPs;
            BlacklistIPList.ItemsSource = _blacklistedIPs;
        }


        private void LoadOrCreateConfig()
        {
            try
            {
                // Ensure App_Data directory exists
                string appDataPath = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }

                // Create new config or load existing
                _config = new SecurityConfig();

                if (!File.Exists(_configPath))
                {
                    // Create default config
                    var defaultConfig = new SecurityConfig
                    {
                        EnableIdsAlerts = true,
                        EnableDosAlerts = true,
                        EnableRansomwareAlerts = true,
                        MonitoredPorts = new List<int> { 80, 443, 8080, 22 },
                        WhitelistedIPs = new List<string> { "127.0.0.1" },
                        BlacklistedIPs = new List<string>(),
                        PrimaryEmail = "",
                        AdditionalRecipients = new List<string>()
                    };

                    // Save default config
                    string jsonString = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_configPath, jsonString);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error initializing configuration: {ex.Message}");
            }
        }

        private void LoadSecurityConfig()
        {
            try
            {
                // Clear existing collections
                _monitoredPorts.Clear();
                _whitelistedIPs.Clear();
                _blacklistedIPs.Clear();
                _emailRecipients.Clear();

                // Load alert settings
                IdsAlert.IsChecked = _config.EnableIdsAlerts;
                AntiDosAlert.IsChecked = _config.EnableDosAlerts;
                RansomwareAlert.IsChecked = _config.EnableRansomwareAlerts;

                // Load monitored ports
                foreach (var port in _config.MonitoredPorts)
                {
                    _monitoredPorts.Add(port.ToString());
                }

                // Load IP lists
                foreach (var ip in _config.WhitelistedIPs)
                {
                    _whitelistedIPs.Add(ip);
                }
                foreach (var ip in _config.BlacklistedIPs)
                {
                    _blacklistedIPs.Add(ip);
                }

                // Load email settings
                PrimaryEmail.Text = _config.PrimaryEmail;
                foreach (var recipient in _config.AdditionalRecipients)
                {
                    _emailRecipients.Add(recipient);
                }

                ShowSuccess("Configuration loaded successfully");
            }
            catch (Exception ex)
            {
                ShowError($"Error loading configuration: {ex.Message}");
            }
        }

        private void HandleProtectionToggle(object sender, RoutedEventArgs e)
        {
            try
            {
                // Define the path to the configuration file
                string path = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "monitoring.json");

                // Check if the config file exists
                if (!File.Exists(path))
                {
                    // Show an error if the file does not exist
                    System.Windows.MessageBox.Show("Configuration file not found. Please ensure 'monitoring.json' exists in the App_Data folder.");
                    return;
                }
                _folderMonitor = new FolderMonitor(path);

                // Start monitoring
                _folderMonitor.StartMonitoring();

                // Notify that monitoring is enabled
                System.Windows.MessageBox.Show("Monitoring enabled.");
            }
            catch (Exception ex)
            {
                // Handle any unexpected errors
                System.Windows.MessageBox.Show($"An error occurred while enabling monitoring: {ex.Message}");
            }
        }


        //private void HandleProtectionToggle(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        // Check if sender is null
        //        if (sender == null)
        //        {
        //            System.Windows.MessageBox.Show("Toggle button not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            return;
        //        }

        //        // Check if it's a ToggleButton
        //        if (!(sender is ToggleButton toggle))
        //        {
        //            System.Windows.MessageBox.Show("Invalid control type.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            return;
        //        }

        //        // Check if _protectionChecker is initialized
        //        if (_status_protection == null)
        //        {
        //            System.Windows.MessageBox.Show("Protection checker not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            return;
        //        }

        //        // Get toggle information
        //        bool isEnabled = toggle.IsChecked ?? false;
        //        string toggleName = toggle.Name;

        //        if (isEnabled)
        //        {
        //            string path = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "monitoring.json");
        //            FolderMonitor f = new FolderMonitor(path);
        //            f.StartMonitoring();


        //            System.Windows.MessageBox.Show("enabled");
        //        }

        //        // Debug information
        //        System.Windows.MessageBox.Show($"Toggle Name: {toggleName}");
        //        System.Windows.MessageBox.Show($"Status: {(isEnabled ? "Enabled" : "Disabled")}");

        //        // Update protection status
        //        _status_protection.SetProtectionStatus(toggleName, isEnabled);

        //    }
        //    catch (Exception ex)
        //    {
        //        System.Windows.MessageBox.Show($"Error handling toggle: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}


        private void SaveConfigurationChanges()
        {
            try
            {
                _config.SaveConfiguration();
                ShowSuccess("Configuration saved successfully");
            }
            catch (Exception ex)
            {
                ShowError($"Error saving configuration: {ex.Message}");
            }
        }

        private void Alert_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_config != null)
            {
                _config.EnableIdsAlerts = IdsAlert.IsChecked ?? false;
                _config.EnableDosAlerts = AntiDosAlert.IsChecked ?? false;
                _config.EnableRansomwareAlerts = RansomwareAlert.IsChecked ?? false;
                SaveConfigurationChanges();
            }
        }


        private void ShowSuccess(string message)
        {
            System.Windows.MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowError(string message)
        {
            System.Windows.MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    

    private void InitializeHomePageData()
        {
            // Initialize Protection Features
            _protectionFeaturesList = new ObservableCollection<string>
            {
                "🛡️ Real-time Protection",
                "🔒 Ransomware Shield",
                "🌐 Network Security",
                "🔍 Behavioral Analysis",
                "⚡ Quick Response",
                "📊 Threat Analytics"
            };

            // Initialize Ransomware Facts
            _ransomwareFactsList = new ObservableCollection<string>
            {
                "⚠️ A ransomware attack occurs every 11 seconds globally",
                "💰 The average ransomware payment increased by 82% in 2021",
                "🏢 60% of organizations were hit by ransomware in 2023",
                "🔐 95% of ransomware attacks are preventable with proper security measures",
                "💻 Most ransomware attacks start with phishing emails",
                "🌍 Ransomware caused $20 billion in damages globally in 2023"
            };

            // Initialize Protection Layers
            _protectionLayersList = new ObservableCollection<ProtectionLayer>
            {
                new ProtectionLayer { Number = "1", Description = "Network Monitoring - Continuous surveillance of network traffic for suspicious activities" },
                new ProtectionLayer { Number = "2", Description = "Behavioral Analysis - Advanced AI-powered detection of unusual system behavior" },
                new ProtectionLayer { Number = "3", Description = "File System Protection - Real-time monitoring of file system changes and encryptions" },
                new ProtectionLayer { Number = "4", Description = "Email Security - Advanced filtering and scanning of incoming emails for threats" },
                new ProtectionLayer { Number = "5", Description = "Backup System - Automated backup system with versioning and quick recovery" },
                new ProtectionLayer { Number = "6", Description = "Access Control - Strict user authentication and permission management" },
                new ProtectionLayer { Number = "7", Description = "Incident Response - Automated threat containment and recovery procedures" }
            };
        }

        // Protection Layer class for data binding
        public class ProtectionLayer
        {
            public string Number { get; set; }
            public string Description { get; set; }
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void SevenLayerProtectionButton_Click(object sender, RoutedEventArgs e)
        {
            var protectionWindow = new SevenLayerProtectionWindow();
            protectionWindow.ShowDialog();
        }

        private void Window_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {

        }

        // Navigation Methods
        private void HideAllPages()
        {
            HomePage.Visibility = Visibility.Collapsed;
            RansomwarePage.Visibility = Visibility.Collapsed;
            DosPage.Visibility = Visibility.Collapsed;
            IdsPage.Visibility = Visibility.Collapsed;
            WafPage.Visibility = Visibility.Collapsed;
            RateLimiterPage.Visibility = Visibility.Collapsed;
            SettingPage.Visibility = Visibility.Collapsed;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            HomePage.Visibility = Visibility.Visible;
        }

        private void RansomwareButton_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            RansomwarePage.Visibility = Visibility.Visible;
        }

        private void Dos_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            DosPage.Visibility = Visibility.Visible;
        }

        private void Ids_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            IdsPage.Visibility = Visibility.Visible;
        }

        private void Waf_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            WafPage.Visibility = Visibility.Visible;
        }

        private void RateLimiter_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            RateLimiterPage.Visibility = Visibility.Visible;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            HideAllPages();
            SettingPage.Visibility = Visibility.Visible;
        }
        private void setting(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Settings have been updated successfully!", "Alert", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void DeleteButton_Click(object sender,RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("deletebutton");

        }

        private void RestoreButton_Click(object sender,RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("deletebutton");

        }

        private void ExportButton_Click(Object sender,RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Export");
        }

        // Port Management
        private void AddPort_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("called add port clicked");
            if (string.IsNullOrWhiteSpace(PortInput.Text))
            {
                ShowWarning("Please enter a port number.");
                return;
            }

            if (int.TryParse(PortInput.Text, out int port))
            {
                if (port >= 0 && port <= 65535)
                {
                    if (!_monitoredPorts.Contains(port.ToString()))
                    {
                        _monitoredPorts.Add(port.ToString());
                        System.Windows.MessageBox.Show("port added");
                        _config.MonitoredPorts.Add(port);
                        _config.SaveConfiguration();
                        PortInput.Clear();
                    }
                    else
                    {
                        ShowWarning("This port is already being monitored.");
                    }
                }
                else
                {
                    ShowWarning("Port number must be between 0 and 65535.");
                }
            }
            else
            {
                ShowWarning("Please enter a valid port number.");
            }
        }

        private void RemovePort_Click(object sender, RoutedEventArgs e)
        {
            if (PortList.SelectedItem != null)
            {
                string selectedPort = PortList.SelectedItem.ToString();
                _monitoredPorts.Remove(selectedPort);
                _config.MonitoredPorts.Remove(int.Parse(selectedPort));
                _config.SaveConfiguration();
            }
            else
            {
                ShowWarning("Please select a port to remove.");
            }
        }

        // IP Whitelist Management
        private void AddWhitelistIP_Click(object sender, RoutedEventArgs e)
        {
            string ip = WhitelistIPInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(ip))
            {
                ShowWarning("Please enter an IP address.");
                return;
            }

            if (IsValidIP(ip))
            {
                if (!_whitelistedIPs.Contains(ip))
                {
                    if (_blacklistedIPs.Contains(ip))
                    {
                        ShowWarning("This IP address is currently blacklisted. Please remove it from the blacklist first.");
                        return;
                    }
                    _whitelistedIPs.Add(ip);
                    _config.WhitelistedIPs.Add(ip);
                    _config.SaveConfiguration();
                    WhitelistIPInput.Clear();
                }
                else
                {
                    ShowWarning("This IP address is already whitelisted.");
                }
            }
            else
            {
                ShowWarning("Please enter a valid IP address.");
            }
        }

        private void RemoveWhitelistIP_Click(object sender, RoutedEventArgs e)
        {
            if (WhitelistIPList.SelectedItem != null)
            {
                string selectedIP = WhitelistIPList.SelectedItem.ToString();
                _whitelistedIPs.Remove(selectedIP);
                _config.WhitelistedIPs.Remove(selectedIP);
                _config.SaveConfiguration();
            }
            else
            {
                ShowWarning("Please select an IP address to remove from the whitelist.");
            }
        }

        // IP Blacklist Management
        private void AddBlacklistIP_Click(object sender, RoutedEventArgs e)
        {
            string ip = BlacklistIPInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(ip))
            {
                ShowWarning("Please enter an IP address.");
                return;
            }

            if (IsValidIP(ip))
            {
                if (!_blacklistedIPs.Contains(ip))
                {
                    if (_whitelistedIPs.Contains(ip))
                    {
                        ShowWarning("This IP address is currently whitelisted. Please remove it from the whitelist first.");
                        return;
                    }
                    _blacklistedIPs.Add(ip);
                    _config.BlacklistedIPs.Add(ip);
                    _config.SaveConfiguration();
                    BlacklistIPInput.Clear();
                }
                else
                {
                    ShowWarning("This IP address is already blacklisted.");
                }
            }
            else
            {
                ShowWarning("Please enter a valid IP address.");
            }
        }

        private void RemoveBlacklistIP_Click(object sender, RoutedEventArgs e)
        {
            if (BlacklistIPList.SelectedItem != null)
            {
                string selectedIP = BlacklistIPList.SelectedItem.ToString();
                _blacklistedIPs.Remove(selectedIP);
                _config.BlacklistedIPs.Remove(selectedIP);
                _config.SaveConfiguration();
            }
            else
            {
                ShowWarning("Please select an IP address to remove from the blacklist.");
            }
        }

        // Email Management
        private void AddRecipient_Click(object sender, RoutedEventArgs e)
        {
            string email = RecipientInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                ShowWarning("Please enter an email address.");
                return;
            }

            if (IsValidEmail(email))
            {
                if (!_emailRecipients.Contains(email))
                {
                    _emailRecipients.Add(email);
                    _config.AdditionalRecipients.Add(email);
                    _config.SaveConfiguration();
                    RecipientInput.Clear();
                }
                else
                {
                    ShowWarning("This email address is already in the list.");
                }
            }
            else
            {
                ShowWarning("Please enter a valid email address.");
            }
        }

        private void RemoveRecipient_Click(object sender, RoutedEventArgs e)
        {
            if (RecipientList.SelectedItem != null)
            {
                string selectedEmail = RecipientList.SelectedItem.ToString();
                _emailRecipients.Remove(selectedEmail);
                _config.AdditionalRecipients.Remove(selectedEmail);
                _config.SaveConfiguration();
            }
            else
            {
                ShowWarning("Please select an email address to remove.");
            }
        }

        private void RemoveGmailAddresses_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var gmailAddresses = _emailRecipients.Where(email =>
                    email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (gmailAddresses.Any())
                {
                    var result = System.Windows.MessageBox.Show(
                        $"Are you sure you want to remove {gmailAddresses.Count} Gmail address(es)?",
                        "Confirm Removal",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        foreach (var email in gmailAddresses)
                        {
                            _emailRecipients.Remove(email);
                            _config.AdditionalRecipients.Remove(email);
                        }
                        _config.SaveConfiguration();
                        ShowSuccess($"Successfully removed {gmailAddresses.Count} Gmail address(es)");
                    }
                }
                else
                {
                    ShowWarning("No Gmail addresses found in the recipient list.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error removing Gmail addresses: {ex.Message}");
            }
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save alert settings
                _config.EnableIdsAlerts = IdsAlert.IsChecked ?? false;
                _config.EnableDosAlerts = AntiDosAlert.IsChecked ?? false;
                _config.EnableRansomwareAlerts = RansomwareAlert.IsChecked ?? false;

                // Save email settings
                if (!string.IsNullOrWhiteSpace(PrimaryEmail.Text) && IsValidEmail(PrimaryEmail.Text))
                {
                    _config.PrimaryEmail = PrimaryEmail.Text.Trim();
                    _config.SaveConfiguration();
                    ShowSuccess("Configuration saved successfully.");
                }
                else
                {
                    ShowWarning("Please enter a valid primary email address.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error saving configuration: {ex.Message}");
            }
        }


 

        // Helper Methods
        private bool IsValidIP(string ipString)
        {
            if (string.IsNullOrWhiteSpace(ipString))
                return false;

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4)
                return false;

            return splitValues.All(r => byte.TryParse(r, out byte tempForParsing));
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }



        private void ShowWarning(string message)
        {
            System.Windows.MessageBox.Show(message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }


    }
}