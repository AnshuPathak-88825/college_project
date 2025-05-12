using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows;
using Newtonsoft.Json;

namespace PacketProtection._0.Services
{
    public class SystemInfo
    {
        public string HostName { get; private set; }
        public string InternalIPAddress { get; private set; }
        public string OSName { get; private set; }
        public string OSVersion { get; private set; }
        public string MachineName { get; private set; }
        public string ProcessorInfo { get; private set; }
        public string TotalRAM { get; private set; }
        public string SystemArchitecture { get; private set; }
        public string LastBootUpTime { get; private set; }

        public SystemInfo()
        {
            CollectSystemInformation();
        }

        private void CollectSystemInformation()
        {
            try
            {
                HostName = Dns.GetHostName();
                MachineName = Environment.MachineName;
                OSName = RuntimeInformation.OSDescription;
                OSVersion = Environment.OSVersion.VersionString;
                SystemArchitecture = RuntimeInformation.OSArchitecture.ToString();
                LastBootUpTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                CollectNetworkInfo();
                ProcessorInfo = GetProcessorInfo();
                TotalRAM = GetTotalRAM();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error collecting system information: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetDefaultValues();
            }
        }

        private void CollectNetworkInfo()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                InternalIPAddress = Array.Find(host.AddressList,
                    ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "Unknown";
            }
            catch
            {
                InternalIPAddress = "Unknown";
            }
        }

        private string GetProcessorInfo()
        {
            return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown Processor";
        }

        private string GetTotalRAM()
        {
            try
            {
                var memoryInfo = GC.GetGCMemoryInfo();
                double ramGB = memoryInfo.TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024);
                return $"{ramGB:F1} GB";
            }
            catch
            {
                return "Unknown RAM";
            }
        }

        private void SetDefaultValues()
        {
            HostName = "Unknown";
            InternalIPAddress = "Unknown";
            OSName = "Unknown";
            OSVersion = "Unknown";
            MachineName = "Unknown";
            ProcessorInfo = "Unknown";
            TotalRAM = "Unknown";
            SystemArchitecture = "Unknown";
            LastBootUpTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    public class SecurityStatus
    {
        public bool IsEnabled { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Details { get; set; }

        public SecurityStatus(bool isEnabled = false, string details = "Not configured")
        {
            IsEnabled = isEnabled;
            LastUpdated = DateTime.Now;
            Details = details;
        }
    }

    public class ProtectionStatus
    {
        public DateTime LastChecked { get; set; }
        public SystemInfo SystemInformation { get; set; }
        public Dictionary<string, SecurityStatus> ProtectionStatuses { get; set; }

        public ProtectionStatus()
        {
            LastChecked = DateTime.Now;
            SystemInformation = new SystemInfo();
            InitializeProtectionStatuses();
        }

        private void InitializeProtectionStatuses()
        {
            ProtectionStatuses = new Dictionary<string, SecurityStatus>
            {
                { "ProtectionToggle", new SecurityStatus() },
                { "DosToggle", new SecurityStatus() },
                { "IdsToggle", new SecurityStatus() },
                { "WafToggle",new SecurityStatus()},
                { "RateLimiterToggle", new SecurityStatus() }
            };
        }
    }

    public class CheckProtection : IDisposable
    {
        private readonly string _jsonFilePath;
        private ProtectionStatus _protectionStatus;
        private readonly object _lockObject = new object();
        private bool _disposed;

        public CheckProtection()
        {
            string appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
            _jsonFilePath = Path.Combine(appDataPath, "protection_status.json");
            InitializeProtectionStatus();
        }

        public void SetProtectionStatus(string protectionName, bool isEnabled, string details = null)
        {
            ThrowIfDisposed();

            lock (_lockObject)
            {
                if (_protectionStatus.ProtectionStatuses.ContainsKey(protectionName))
                {
                    _protectionStatus.ProtectionStatuses[protectionName] = new SecurityStatus(
                        isEnabled,
                        details ?? (isEnabled ? "Active" : "Disabled")
                    );
                    SaveProtectionStatus();
                }
                else
                {
                    MessageBox.Show($"Protection name '{protectionName}' does not exist.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }


        public Dictionary<string, SecurityStatus> GetProtectionStatuses()
        {
            ThrowIfDisposed();

            lock (_lockObject)
            {
                return new Dictionary<string, SecurityStatus>(_protectionStatus.ProtectionStatuses);
            }
        }

        public SecurityStatus GetProtectionStatus(string protectionName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(protectionName))
                throw new ArgumentNullException(nameof(protectionName));

            lock (_lockObject)
            {
                return _protectionStatus.ProtectionStatuses.TryGetValue(protectionName, out var status)
                    ? status
                    : null;
            }
        }

        public SystemInfo GetSystemInfo()
        {
            ThrowIfDisposed();
            return _protectionStatus.SystemInformation;
        }

        public void RefreshSystemInfo()
        {
            ThrowIfDisposed();

            lock (_lockObject)
            {
                _protectionStatus.SystemInformation = new SystemInfo();
                SaveProtectionStatus();
            }
        }

        private void InitializeProtectionStatus()
        {
            try
            {
                string directory = Path.GetDirectoryName(_jsonFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                lock (_lockObject)
                {
                    if (File.Exists(_jsonFilePath))
                    {
                        LoadProtectionStatus();
                    }
                    else
                    {
                        _protectionStatus = new ProtectionStatus();
                        SaveProtectionStatus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize protection status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _protectionStatus = new ProtectionStatus();
            }
        }

        private void LoadProtectionStatus()
        {
            try
            {
                string jsonContent = File.ReadAllText(_jsonFilePath);
                _protectionStatus = JsonConvert.DeserializeObject<ProtectionStatus>(jsonContent)
                    ?? new ProtectionStatus();
                _protectionStatus.SystemInformation = new SystemInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading protection status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _protectionStatus = new ProtectionStatus();
            }
        }

        private void SaveProtectionStatus()
        {
            try
            {
                _protectionStatus.LastChecked = DateTime.Now;
                string jsonContent = JsonConvert.SerializeObject(_protectionStatus, Formatting.Indented);
                File.WriteAllText(_jsonFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving protection status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CheckProtection));
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    SaveProtectionStatus();
                }
                catch
                {
                    // Ignore errors during disposal
                }
                _disposed = true;
            }
        }
    }
}
