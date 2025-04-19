using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Security.Principal;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;

namespace RealTimeProtection
{
    public class NotificationManager
    {
        private System.Windows.Forms.NotifyIcon _notifyIcon;

        public NotificationManager()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Visible = true,
                Icon = SystemIcons.Information,
                BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info
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

    public partial class FolderMonitor
    {
        private readonly object _scanLock = new object();
        private FileSystemWatcher _fileWatcher;
        private Process _clamdProcess;
        private string _quarantinePath = @"C:\Quarantine";
        private readonly string _clamdScanPath = @"C:\Program Files\ClamAV\clamdscan.exe";
        private readonly string _clamdExePath = @"C:\Program Files\ClamAV\clamd.exe";
        private readonly string _logFilePath = @"C:\Logs\real_time_protection_log.txt";
        private const string NotificationBackgroundColor = "#2C2C2C"; // Dark theme background
        private const string NotificationTextColor = "#FFFFFF"; // White text color
        NotificationManager notificationManager = new NotificationManager();
        private List<FileSystemWatcher> _fileWatchers = new List<FileSystemWatcher>(); // Store watchers for all directories
        private readonly string _jsonFilePath;
        public FolderMonitor(string _jsonFilePath)
        {
            //InitializeComponent();
            CheckAndElevatePrivileges();
            //ApplyDarkTheme(); // Applying dark theme to the window
            //_jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "monitoring.json"); // Use current directory
            this._jsonFilePath = _jsonFilePath;
        }

        private void ShowWindowsNotification(string title, string message)
        {
            notificationManager.ShowNotification(title, message);
        }

        private void CheckAndElevatePrivileges()
        {
            if (!IsRunAsAdministrator())
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = Process.GetCurrentProcess().MainModule.FileName,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                try
                {
                    Process.Start(processInfo);
                    System.Windows.Application.Current.Shutdown();
                }
                catch (Exception)
                {
                    System.Windows.MessageBox.Show("This application requires administrator privileges to run.");
                    System.Windows.Application.Current.Shutdown();
                }
            }
        }

        private bool IsRunAsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void ScanExistingFiles(string directory)
        {
            try
            {
                var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    ScanFile(file);
                }
                LogEvent($"Completed initial scan of {files.Length} files in {directory}");
            }
            catch (Exception ex)
            {
                LogEvent($"Error during initial directory scan: {ex.Message}");
            }
        }

        private void ScanFile(string filePath)
        {
            if (!File.Exists(filePath)) return;

            Task.Run(() =>
            {
                try
                {
                    string scanResult = ScanWithClamAV(filePath);
                    if (scanResult.Contains("FOUND"))
                    {
                        ShowWindowsNotification("Threat Detected", scanResult);
                        QuarantineFile(filePath);
                        SendEmailAlert(filePath, scanResult);
                    }
                }
                catch (Exception ex)
                {
                    LogEvent($"Error scanning file {filePath}: {ex.Message}");
                }
            });
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                Thread.Sleep(1000); // Add delay to ensure file is fully written

                if (File.Exists(e.FullPath))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        LogEvent($"File {e.ChangeType}: {e.FullPath}");
                        ScanFile(e.FullPath);
                    });
                }
            }
            catch (Exception ex)
            {
                LogEvent($"Error handling file change: {ex.Message}");
            }
        }

        private List<MonitoringFolder> GetDirectoriesFromJson()
        {
            try
            {
                // Read the JSON file
                var jsonData = File.ReadAllText(_jsonFilePath);
                var config = JsonConvert.DeserializeObject<Config>(jsonData);

                return config?.MonitoringFolders?.Where(f => f.IsActive).ToList();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error reading the JSON file: {ex.Message}");
                return null;
            }
        }


        private void StartClamdIfNeeded()
        {
            try
            {
                // Check if clamd.exe is running by searching for it in the process list
                var processes = Process.GetProcessesByName("clamd");

                if (processes.Length == 0)
                {
                    // If clamd.exe is not running, start it
                    LogEvent("ClamAV daemon (clamd.exe) is not running. Starting clamd...");
                    StartClamd();
                }
                else
                {
                    LogEvent("ClamAV daemon (clamd.exe) is already running.");
                }
            }
            catch (Exception ex)
            {
                LogEvent($"Error while checking or starting ClamAV: {ex.Message}");
            }
        }

        private void StartClamd()
        {
            try
            {
                // Start clamd.exe process
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _clamdExePath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process.Start(processStartInfo);
                LogEvent("ClamAV daemon (clamd.exe) started successfully.");
            }
            catch (Exception ex)
            {
                LogEvent($"Error starting clamd.exe: {ex.Message}");
            }
        }



        public void StartMonitoring()
        {
            var directoriesToMonitor = GetDirectoriesFromJson();

            if (directoriesToMonitor == null || !directoriesToMonitor.Any())
            {
                System.Windows.MessageBox.Show("No valid directories to monitor in the JSON file.");
                return;
            }

            StartClamdIfNeeded();  // Start clamd.exe first

            // Initial scan and setup watchers for each active directory
            foreach (var directory in directoriesToMonitor)
            {
                if (Directory.Exists(directory.Path) && directory.IsActive)
                {
                    ScanExistingFiles(directory.Path);

                    // Create and configure a FileSystemWatcher for each directory
                    var fileWatcher = new FileSystemWatcher(directory.Path)
                    {
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                        EnableRaisingEvents = true
                    };
                    fileWatcher.Created += OnFileChanged;
                    fileWatcher.Changed += OnFileChanged;

                    _fileWatchers.Add(fileWatcher); // Add watcher to the list

                    LogEvent($"Started monitoring directory: {directory.Path}");
                    ShowCustomNotification("Monitoring Started", $"Started monitoring: {directory.Path}");
                }
            }

            // Additional feature: Start monitoring new paths if they are added to the JSON
            MonitorPathsInJsonFile();
        }

      
        public class Config
        {
            public List<MonitoringFolder> MonitoringFolders { get; set; }
            public List<object> PremiumFolders { get; set; }
        }

        public class MonitoringFolder
        {
            public string Path { get; set; }
            public DateTime DateAdded { get; set; }
            public bool IsPremium { get; set; }
            public bool IsActive { get; set; }
        }

        private void StopMonitoring_Click(object sender, RoutedEventArgs e)
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.EnableRaisingEvents = false;
                _fileWatcher.Dispose();
                _fileWatcher = null;

                LogEvent("Stopped monitoring.");
                ShowCustomNotification("Monitoring Stopped", "Monitoring stopped.");
            }
        }

        private void ShowCustomNotification(string title, string message)
        {
            var notification = new System.Windows.Controls.TextBlock
            {
                Text = $"{title}\n{message}",
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 44, 44)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)),
                TextWrapping = TextWrapping.Wrap,
                Padding = new System.Windows.Thickness(20),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 16
            };

            // Create floating notification container
            var container = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 44, 44)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)),
                BorderThickness = new System.Windows.Thickness(2),
                Padding = new System.Windows.Thickness(10),
                Child = notification,
                Width = 250,
                Height = 100
            };
        }

        private string ScanWithClamAV(string filePath)
        {
            lock (_scanLock)
            {
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = _clamdScanPath,
                            Arguments = $"--stdout \"{filePath}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    string result = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return result;
                }
                catch (Exception ex)
                {
                    LogEvent($"Error during ClamAV scan for {filePath}: {ex.Message}");
                    return $"Error during scan: {ex.Message}";
                }
            }
        }

        private void QuarantineFile(string filePath)
        {
            try
            {
                // Ensure the quarantine directory exists
                if (!Directory.Exists(_quarantinePath))
                {
                    Directory.CreateDirectory(_quarantinePath);
                }

                // Generate a unique filename using DateTime.Now.Ticks to avoid conflicts
                string uniqueQuarantineFilePath = Path.Combine(_quarantinePath, $"{Path.GetFileName(filePath)}_{DateTime.Now.Ticks}.elf");

                // Move the file to the quarantine directory with the unique name
                File.Move(filePath, uniqueQuarantineFilePath);

                // Log the event of quarantining the file
                LogEvent($"File quarantined: {filePath}");
            }
            catch (Exception ex)
            {
                // Log any errors that occur during the process
                LogEvent($"Error quarantining file {filePath}: {ex.Message}");
            }
        }


        private void SendEmailAlert(string filePath, string scanResult)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.yourserver.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("your-email@example.com", "your-email-password"),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("your-email@example.com"),
                    Subject = $"ClamAV Alert: Virus detected in {filePath}",
                    Body = $"{scanResult} - File: {filePath}",
                    IsBodyHtml = false,
                };

                mailMessage.To.Add("recipient@example.com");
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                LogEvent($"Error sending email alert: {ex.Message}");
            }
        }

        public void reload()
        {
            try
            {
                MonitorPathsInJsonFile();

            }
            catch(Exception ex) { 
            

                System.Windows.MessageBox.Show(ex.Message); 
                LogEvent($"Error starting file watcher: {ex.Message}");


            }

        }
        private void MonitorPathsInJsonFile()
        {
            // Increment the method call count
            //methodCallCount++;
            System.Windows.MessageBox.Show("called changed the json");

            // Log the number of times this method has been called
            //LogEvent($"MonitorPathsInJsonFile called {methodCallCount} times.");

            // Watch for changes in the JSON file
            FileSystemWatcher jsonFileWatcher = new FileSystemWatcher(Path.GetDirectoryName(_jsonFilePath))
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            jsonFileWatcher.Changed += (sender, args) =>
            {
                // Reload the paths when the JSON file is modified
                var newPaths = GetDirectoriesFromJson(); // Ensure this method is returning valid paths
                if (newPaths != null)
                {
                    // Log the paths loaded from JSON
                    LogEvent($"Paths loaded from JSON: {string.Join(", ", newPaths.Select(d => d.Path))}");

                    // Stop watching removed directories
                    foreach (var directory in _fileWatchers.ToList())
                    {
                        var matchingFolder = newPaths.FirstOrDefault(f => f.Path == directory.Path);
                        if (matchingFolder == null)
                        {
                            // Stop watching this directory
                            directory.EnableRaisingEvents = false; // Disable events for this folder
                            directory.Dispose(); // Dispose the watcher
                            _fileWatchers.Remove(directory); // Remove from list
                            LogEvent($"Stopped monitoring directory: {directory.Path}");
                        }
                    }

                    // Add watchers for newly added or modified paths
                    foreach (var directory in newPaths.Where(d => !d.IsActive && Directory.Exists(d.Path)))
                    {
                        // Check if directory exists and is not already monitored
                        if (!_fileWatchers.Any(f => f.Path == directory.Path))
                        {
                            ScanExistingFiles(directory.Path); // Scan existing files

                            // Create a new watcher for this directory
                            var fileWatcher = new FileSystemWatcher(directory.Path)
                            {
                                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                                EnableRaisingEvents = true
                            };

                            fileWatcher.Created += OnFileChanged;
                            fileWatcher.Changed += OnFileChanged;

                            _fileWatchers.Add(fileWatcher); // Add watcher to the list
                            LogEvent($"Started monitoring new directory: {directory.Path}");
                            ShowCustomNotification("Monitoring Started", $"Started monitoring: {directory.Path}");
                        }
                    }
                }
            };

            // Optionally, handle errors with try-catch
            try
            {
                jsonFileWatcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                LogEvent($"Error starting file watcher: {ex.Message}");
            }
        }


        private void LogEvent(string message)
        {
            try
            {
                string logMessage = $"{DateTime.Now}: {message}";
                File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log: {ex.Message}");
            }
        }
    }
}
