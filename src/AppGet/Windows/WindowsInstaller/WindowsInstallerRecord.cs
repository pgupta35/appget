﻿using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace AppGet.Windows.WindowsInstaller
{
    [DebuggerDisplay("{Id}")]
    public class WindowsInstallerRecord
    {
        public bool Is64 { get; set; }
        public string Id { get; set; }
        public bool IsUpgradeNode { get; set; }
        public string Hive { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> Values { get; set; }
    }
}