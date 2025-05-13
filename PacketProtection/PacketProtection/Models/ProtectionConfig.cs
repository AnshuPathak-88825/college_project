using System;
using System.Collections.Generic;

namespace PacketProtection._0.Models
{
    public class ProtectionConfig
    {
        public List<ProtectedFolderInfo> MonitoringFolders { get; set; } = new List<ProtectedFolderInfo>();
        public List<ProtectedFolderInfo> PremiumFolders { get; set; } = new List<ProtectedFolderInfo>();
        public DateTime LastUpdated { get; set; }
    }

    public class ProtectedFolderInfo
    {
        public string Path { get; set; }
        public DateTime DateAdded { get; set; }
        public bool IsPremium { get; set; }
        public bool IsActive { get; set; }
    }
}