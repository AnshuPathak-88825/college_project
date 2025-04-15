using System;
using System.IO;
using System.Windows;
using Newtonsoft.Json;
using CyberPanacea_V2._0.Models;
using System.Collections.ObjectModel;


namespace CyberPanacea_V2._0.Services
{
    public class ConfigurationService
    {
        private const string CONFIG_DIRECTORY = "App_Data";
        private const string SECURITY_CONFIG_FILE = "security_config.json";
        private const string PROTECTION_CONFIG_FILE = "7_layer_config.json";


        private void EnsureAppDataFolderExists()
        {
            string appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
        }

        private static string GetConfigFilePath(string fileName)
        {
            string appDataPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                CONFIG_DIRECTORY
            );
            // Ensure directory exists
            Directory.CreateDirectory(appDataPath);
            return Path.Combine(appDataPath, fileName);
        }

        #region Security Configuration
        public static SecurityConfig LoadSecurityConfiguration()
        {
            try
            {
                string configPath = GetConfigFilePath(SECURITY_CONFIG_FILE);
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    return JsonConvert.DeserializeObject<SecurityConfig>(json)
                           ?? new SecurityConfig();
                }
                return new SecurityConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading security configuration: {ex.Message}",
                    "Configuration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return new SecurityConfig();
            }
        }

        public static void SaveSecurityConfiguration(SecurityConfig config)
        {
            try
            {
                string configPath = GetConfigFilePath(SECURITY_CONFIG_FILE);
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error saving security configuration: {ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
        #endregion

        #region Protection Configuration
        public static ProtectionConfig LoadProtectionConfiguration()
        {
            try
            {
                string configPath = GetConfigFilePath(PROTECTION_CONFIG_FILE);
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonConvert.DeserializeObject<ProtectionConfig>(json);
                    return config ?? new ProtectionConfig();
                }
                return new ProtectionConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading protection configuration: {ex.Message}",
                    "Configuration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return new ProtectionConfig();
            }
        }

        public static void SaveProtectionConfiguration(ObservableCollection<ProtectedFolder> monitoringFolders,
                                                     ObservableCollection<ProtectedFolder> premiumFolders)
        {
            try
            {
                var config = new ProtectionConfig
                {
                    LastUpdated = DateTime.Now,
                    MonitoringFolders = ConvertToFolderInfoList(monitoringFolders),
                    PremiumFolders = ConvertToFolderInfoList(premiumFolders)
                };

                string configPath = GetConfigFilePath(PROTECTION_CONFIG_FILE);
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error saving protection configuration: {ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private static System.Collections.Generic.List<ProtectedFolderInfo> ConvertToFolderInfoList(
            ObservableCollection<ProtectedFolder> folders)
        {
            var folderInfoList = new System.Collections.Generic.List<ProtectedFolderInfo>();
            foreach (var folder in folders)
            {
                folderInfoList.Add(new ProtectedFolderInfo
                {
                    Path = folder.Path,
                    DateAdded = folder.DateAdded,
                    IsPremium = folder.IsPremium,
                    IsActive = folder.IsActive
                });
            }
            return folderInfoList;
        }

        public static (ObservableCollection<ProtectedFolder> monitoring,
                      ObservableCollection<ProtectedFolder> premium) LoadProtectionFolders()
        {
            var config = LoadProtectionConfiguration();
            var monitoringFolders = new ObservableCollection<ProtectedFolder>(
                ConvertFromFolderInfoList(config.MonitoringFolders));
            var premiumFolders = new ObservableCollection<ProtectedFolder>(
                ConvertFromFolderInfoList(config.PremiumFolders));

            return (monitoringFolders, premiumFolders);
        }

        private static System.Collections.Generic.List<ProtectedFolder> ConvertFromFolderInfoList(
            System.Collections.Generic.List<ProtectedFolderInfo> folderInfoList)
        {
            var folders = new System.Collections.Generic.List<ProtectedFolder>();
            foreach (var info in folderInfoList)
            {
                var folder = new ProtectedFolder(info.Path, info.IsPremium)
                {
                    DateAdded = info.DateAdded,
                    IsActive = info.IsActive
                };
                folders.Add(folder);
            }
            return folders;
        }
        #endregion

        #region Backup and Recovery
        public static void BackupConfigurations()
        {
            try
            {
                string backupDir = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    CONFIG_DIRECTORY,
                    "Backups",
                    DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")
                );
                Directory.CreateDirectory(backupDir);

                string securityConfigPath = GetConfigFilePath(SECURITY_CONFIG_FILE);
                string protectionConfigPath = GetConfigFilePath(PROTECTION_CONFIG_FILE);

                if (File.Exists(securityConfigPath))
                {
                    File.Copy(
                        securityConfigPath,
                        Path.Combine(backupDir, SECURITY_CONFIG_FILE)
                    );
                }

                if (File.Exists(protectionConfigPath))
                {
                    File.Copy(
                        protectionConfigPath,
                        Path.Combine(backupDir, PROTECTION_CONFIG_FILE)
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error creating backup: {ex.Message}",
                    "Backup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        public static void ValidateConfigurations()
        {
            try
            {
                var securityConfig = LoadSecurityConfiguration();
                var protectionConfig = LoadProtectionConfiguration();

                // Validate security config
                if (securityConfig.MonitoredPorts == null)
                    securityConfig.MonitoredPorts = new System.Collections.Generic.List<int>();
                if (securityConfig.WhitelistedIPs == null)
                    securityConfig.WhitelistedIPs = new System.Collections.Generic.List<string>();
                if (securityConfig.BlacklistedIPs == null)
                    securityConfig.BlacklistedIPs = new System.Collections.Generic.List<string>();
                if (securityConfig.AdditionalRecipients == null)
                    securityConfig.AdditionalRecipients = new System.Collections.Generic.List<string>();

                // Validate protection config
                if (protectionConfig.MonitoringFolders == null)
                    protectionConfig.MonitoringFolders = new System.Collections.Generic.List<ProtectedFolderInfo>();
                if (protectionConfig.PremiumFolders == null)
                    protectionConfig.PremiumFolders = new System.Collections.Generic.List<ProtectedFolderInfo>();

                // Save validated configs
                SaveSecurityConfiguration(securityConfig);
                SaveProtectionConfiguration(
                    new ObservableCollection<ProtectedFolder>(ConvertFromFolderInfoList(protectionConfig.MonitoringFolders)),
                    new ObservableCollection<ProtectedFolder>(ConvertFromFolderInfoList(protectionConfig.PremiumFolders))
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error validating configurations: {ex.Message}",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
        #endregion
    }
}