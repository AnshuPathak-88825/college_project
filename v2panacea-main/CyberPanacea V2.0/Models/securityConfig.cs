using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PacketProtection._0.Models
{
    // Separate data model for serialization
    public class SecurityConfigData
    {
        public bool EnableIdsAlerts { get; set; }
        public bool EnableDosAlerts { get; set; }
        public bool EnableRansomwareAlerts { get; set; }
        public List<int> MonitoredPorts { get; set; }
        public List<string> WhitelistedIPs { get; set; }
        public List<string> BlacklistedIPs { get; set; }
        public string PrimaryEmail { get; set; }
        public List<string> AdditionalRecipients { get; set; }
    }

    public class SecurityConfig
    {
        private readonly string _configPath;
        private const string CONFIG_FILENAME = "monitoring_ports.json";
        private SecurityConfigData _data;

        public bool EnableIdsAlerts
        {
            get => _data.EnableIdsAlerts;
            set => _data.EnableIdsAlerts = value;
        }
        public bool EnableDosAlerts
        {
            get => _data.EnableDosAlerts;
            set => _data.EnableDosAlerts = value;
        }
        public bool EnableRansomwareAlerts
        {
            get => _data.EnableRansomwareAlerts;
            set => _data.EnableRansomwareAlerts = value;
        }
        public List<int> MonitoredPorts
        {
            get => _data.MonitoredPorts;
            set => _data.MonitoredPorts = value;
        }
        public List<string> WhitelistedIPs
        {
            get => _data.WhitelistedIPs;
            set => _data.WhitelistedIPs = value;
        }
        public List<string> BlacklistedIPs
        {
            get => _data.BlacklistedIPs;
            set => _data.BlacklistedIPs = value;
        }
        public string PrimaryEmail
        {
            get => _data.PrimaryEmail;
            set => _data.PrimaryEmail = value;
        }
        public List<string> AdditionalRecipients
        {
            get => _data.AdditionalRecipients;
            set => _data.AdditionalRecipients = value;
        }

        public SecurityConfig()
        {
            _configPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", CONFIG_FILENAME);
            _data = new SecurityConfigData();
            InitializeDefaultValues();
            EnsureAppDataFolderExists();
            LoadConfiguration();
        }

        private void EnsureAppDataFolderExists()
        {
            string appDataPath = Path.GetDirectoryName(_configPath);
            if (!Directory.Exists(appDataPath))
            {
                try
                {
                    Directory.CreateDirectory(appDataPath);
                }
                catch (Exception ex)
                {
                    throw new DirectoryNotFoundException($"Failed to create App_Data directory: {ex.Message}");
                }
            }
        }

        private void InitializeDefaultValues()
        {
            _data.MonitoredPorts = new List<int> { 80, 443, 8080, 22 };
            _data.WhitelistedIPs = new List<string> { "127.0.0.1" };
            _data.BlacklistedIPs = new List<string>();
            _data.AdditionalRecipients = new List<string>();
            _data.EnableIdsAlerts = true;
            _data.EnableDosAlerts = true;
            _data.EnableRansomwareAlerts = true;
            _data.PrimaryEmail = string.Empty;
        }

        public void SaveConfiguration()
        {
            try
            {
                EnsureAppDataFolderExists();
                var jsonString = JsonSerializer.Serialize(_data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_configPath, jsonString);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string jsonString = File.ReadAllText(_configPath);
                    var loadedData = JsonSerializer.Deserialize<SecurityConfigData>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true
                    });

                    if (loadedData != null)
                    {
                        _data = loadedData;

                        // Ensure no null collections
                        _data.MonitoredPorts ??= new List<int>();
                        _data.WhitelistedIPs ??= new List<string>();
                        _data.BlacklistedIPs ??= new List<string>();
                        _data.AdditionalRecipients ??= new List<string>();
                        _data.PrimaryEmail ??= string.Empty;
                    }
                }
                else
                {
                    SaveConfiguration();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration: {ex.Message}", ex);
            }
        }

        public bool Validate()
        {
            if (_data.MonitoredPorts == null || _data.MonitoredPorts.Count == 0)
                return false;

            if (_data.MonitoredPorts.Exists(p => p <= 0 || p > 65535))
                return false;

            if (_data.WhitelistedIPs == null || _data.BlacklistedIPs == null)
                return false;

            return true;
        }

        public void ResetToDefaults()
        {
            InitializeDefaultValues();
            SaveConfiguration();
        }
    }
}