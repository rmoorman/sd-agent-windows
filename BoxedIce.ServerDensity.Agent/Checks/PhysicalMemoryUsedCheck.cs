﻿using System;
using System.Collections.Generic;
using System.Management;
using System.Text;
using BoxedIce.ServerDensity.Agent.PluginSupport;

namespace BoxedIce.ServerDensity.Agent.Checks
{
    /// <summary>
    /// Class for physical memory usage checks.
    /// </summary>
    public class PhysicalMemoryUsedCheck : ICheck
    {
        #region ICheck Members

        public string Key
        {
            get { return "memPhysUsed"; }
        }

        public object DoCheck()
        {
            ulong used = 0;
            using (var query = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem"))
            {
                var list = query.Get();
                using (list)
                {
                    foreach (var memory in list)
                    {
                        ulong total = (ulong)memory.GetPropertyValue("TotalVisibleMemorySize");
                        ulong free = (ulong)memory.GetPropertyValue("FreePhysicalMemory");
                        used = (total - free) / 1024;
                    }
                    return used;
                }
            }
        }

        #endregion
    }
}
