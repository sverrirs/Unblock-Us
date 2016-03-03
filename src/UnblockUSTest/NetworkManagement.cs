﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ROOT.CIMV2.Win32;

namespace UnblockUSTest
{
    using System;
    using System.Management;

    namespace WindowsFormsApplication_CS
    {
        class NetworkManagement
        {
            /// <summary>
            /// Returns a list of all the network interface class names that are currently enabled in the system
            /// </summary>
            /// <returns>list of nic names</returns>
            public static string[] GetAllNicDescriptions()
            {
                List<string> nics = new List<string>();

                using (var networkConfigMng = new ManagementClass("Win32_NetworkAdapterConfiguration"))
                {
                    using (var networkConfigs = networkConfigMng.GetInstances())
                    {
                        foreach (var config in networkConfigs.Cast<ManagementObject>()
                                                                           .Where(mo => (bool)mo["IPEnabled"])
                                                                           .Select(x=> new NetworkAdapterConfiguration(x)))
                        {
                            nics.Add(config.Description);
                        }
                    }
                }

                return nics.ToArray();
            }

            /// <summary>
            /// Set's the DNS Server of the local machine
            /// </summary>
            /// <param name="nicDescription">The full description of the network interface class</param>
            /// <param name="dnsServers">Comma seperated list of DNS server addresses</param>
            /// <remarks>Requires a reference to the System.Management namespace</remarks>
            public static bool SetNameservers(string nicDescription, string[] dnsServers, bool restart = false)
            {
                using (ManagementClass networkConfigMng = new ManagementClass("Win32_NetworkAdapterConfiguration"))
                {
                    using (ManagementObjectCollection networkConfigs = networkConfigMng.GetInstances())
                    {
                        foreach (ManagementObject mboDNS in networkConfigs.Cast<ManagementObject>().Where(mo => (bool)mo["IPEnabled"] && (string)mo["Description"] == nicDescription))
                        {
                            // NAC class was generated by opening a developer console and entering:
                            // mgmtclassgen Win32_NetworkAdapterConfiguration -p NetworkAdapterConfiguration.cs
                            // See: http://blog.opennetcf.com/2008/06/24/disableenable-network-connections-under-vista/

                            using (NetworkAdapterConfiguration config = new NetworkAdapterConfiguration(mboDNS))
                            {
                                if (config.SetDNSServerSearchOrder(dnsServers) == 0)
                                {
                                    RestartNetworkAdapter(nicDescription);
                                }
                            }
                        }
                    }
                }

                return false;
            }

            /// <summary>
            /// Restarts a given Network adapter
            /// </summary>
            /// <param name="nicDescription">The full description of the network interface class</param>
            public static void RestartNetworkAdapter(string nicDescription)
            {
                using (ManagementClass networkConfigMng = new ManagementClass("Win32_NetworkAdapter"))
                {
                    using (ManagementObjectCollection networkConfigs = networkConfigMng.GetInstances())
                    {
                        foreach (ManagementObject mboDNS in networkConfigs.Cast<ManagementObject>().Where(mo=> (string)mo["Description"] == nicDescription))
                        {
                            // NA class was generated by opening dev console and entering
                            // mgmtclassgen Win32_NetworkAdapter -p NetworkAdapter.cs
                            using (NetworkAdapter adapter = new NetworkAdapter(mboDNS))
                            {
                                adapter.Disable();
                                adapter.Enable();
                                Thread.Sleep(4000); // Wait 5 secs until exiting, will give enough time to re-connect
                                return;
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Get's the DNS Server of the local machine
            /// </summary>
            /// <param name="nicDescription">The full description of the network interface class</param>
            public static string[] GetNameservers(string nicDescription)
            {
                using (var networkConfigMng = new ManagementClass("Win32_NetworkAdapterConfiguration"))
                {
                    using (var networkConfigs = networkConfigMng.GetInstances())
                    {
                        foreach (var config  in networkConfigs.Cast<ManagementObject>()
                                                              .Where(mo => (bool)mo["IPEnabled"] && (string)mo["Description"] == nicDescription)
                                                              .Select( x => new NetworkAdapterConfiguration(x)))
                        {
                            return config.DNSServerSearchOrder;
                        }
                    }
                }

                return null;
            }

            /// <summary>
            /// Set's a new IP Address and it's Submask of the local machine
            /// </summary>
            /// <param name="nicDescription">The full description of the network interface class</param>
            /// <param name="ipAddresses">The IP Address</param>
            /// <param name="subnetMask">The Submask IP Address</param>
            /// <param name="gateway">The gateway.</param>
            /// <remarks>Requires a reference to the System.Management namespace</remarks>
            public static void SetIP(string nicDescription, string[] ipAddresses, string subnetMask, string gateway)
            {
                using (var networkConfigMng = new ManagementClass("Win32_NetworkAdapterConfiguration"))
                {
                    using (var networkConfigs = networkConfigMng.GetInstances())
                    {
                        foreach (var config in networkConfigs.Cast<ManagementObject>()
                                                                       .Where(mo => (bool)mo["IPEnabled"] && (string)mo["Description"] == nicDescription)
                                                                       .Select( x=> new NetworkAdapterConfiguration(x)))
                        {
                            // Set the new IP and subnet masks if needed
                            config.EnableStatic(ipAddresses, Array.ConvertAll(ipAddresses, _ => subnetMask));
                            
                            // Set mew gateway if needed
                            if (!String.IsNullOrEmpty(gateway))
                            {
                                config.SetGateways(new[] {gateway}, new ushort[] {1});
                            }
                        }
                    }
                }
            }

        }
    }
}
