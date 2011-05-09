// Copyright 2011 Murray Grant
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Management;
using System.IO;
using Microsoft.Xna.Framework;
using MurrayGrant.BabyGame.Helpers;

namespace MurrayGrant.BabyGame.Services
{
    /// <summary>
    /// Publishes an exception to Murray's Exception database.
    /// </summary>
    public class ExceptionPublisher
    {
        public Game Game { get; private set; }

        public ExceptionPublisher(Game g)
        {
            this.Game = g;
        }
        public void CreateAndPublish(Exception ex)
        {
            var detail = this.CreateErrorDetails(ex);
            this.Publish(detail);
        }

        public void SaveToDesktop(ExceptionAndComputerDetail detailsToPublish)
        {
            using (var file = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "BabyBashXnaError.txt"), FileMode.Create, FileAccess.Write, FileShare.None))
                detailsToPublish.ToStream(file);
        }
        public void Publish(ExceptionAndComputerDetail detailsToPublish)
        {
            // Save the exception to the desktop until I get remote publishing working.
            this.SaveToDesktop(detailsToPublish);

            // TODO: send it to a webservice or post on the issue tracker on CodePlex.
        }

        public ExceptionAndComputerDetail CreateErrorDetails(Exception ex)
        {
            return new ExceptionAndComputerDetail(ex, "Baby Bash XNA");
        }
    }

    public class ExceptionAndComputerDetail
    {
        public DateTimeOffset Timestamp { get; set; }
        public String Product { get; set; }
        public Version Version { get; set; }
        public Exception Exception { get; set; }

        public ExceptionAndComputerDetail(Exception ex, String product)
        {
            this.Exception = ex;
            this.Product = product;
            this.Version = Helper.GetApplicationVesion();
            this.Timestamp = DateTimeOffset.UtcNow;
        }
        public void ToStream(Stream s)
        {
            var result = new StreamWriter(s, Encoding.UTF8);
            result.Write("TimestampGMT: {0:o} {1}", this.Timestamp, Environment.NewLine);
            result.Write("TimestampLocal: {0:o} {1}", this.Timestamp.ToLocalTime(), Environment.NewLine);
            result.Write("Product: {0}{1}", this.Product, Environment.NewLine);
            result.Write("Version: {0}{1}", this.Version, Environment.NewLine);
            result.WriteLine();
            if (this.Exception != null)
            {
                result.Write("EXCEPTION{0}{1}{0}", Environment.NewLine, this.Exception.ToFullString());
                result.WriteLine();
            }
            this.CreateComputerDetails(result);
            result.WriteLine();
        }
        public override string ToString()
        {
            var result = new StringBuilder();
            result.AppendFormat("Timestamp: {0:o} GMT{1}", this.Timestamp, Environment.NewLine);
            result.AppendFormat("Product: {0}{1}", this.Product, Environment.NewLine);
            result.AppendFormat("Version: {0}{1}", this.Version, Environment.NewLine);
            result.AppendLine();
            result.AppendFormat("EXCEPTION{0}{1}{0}", Environment.NewLine, Exception.ToFullString());
            result.AppendLine();
            result.AppendLine(this.CreateComputerDetails());
            return result.ToString();
        }

        public void CreateComputerDetails(TextWriter result)
        {
            var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
            var screens = System.Windows.Forms.Screen.AllScreens;
            var drives = System.IO.DriveInfo.GetDrives();
            using (var procInfo = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            using (var soundInfo = new ManagementObjectSearcher("SELECT * FROM Win32_SoundDevice"))
            using (var baseboard = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
            using (var computerSystem = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
            using (var videoController = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
            using (var osInfo = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
            using (var experienceInfo = new ManagementObjectSearcher("SELECT * FROM Win32_WinSAT WHERE TimeTaken='MostRecentAssessment'"))
            using (var proc = System.Diagnostics.Process.GetCurrentProcess())
            {
                result.WriteLine("WINDOWS");
                using (var item = computerSystem.Get().Cast<ManagementBaseObject>().First())
                    result.Write("Computer: {0} {1}{2}", item["Manufacturer"], item["Model"], Environment.NewLine);
                result.Write("WindowsVersion: {0}{1}", Environment.OSVersion.VersionString, Environment.NewLine);
                using (var item = osInfo.Get().Cast<ManagementBaseObject>().First())
                {
                    result.Write("WindowsDetail: {0}{1}", item["Name"], Environment.NewLine);
                    result.Write("TotalRam: {0:N0}kB{1}", item["TotalVisibleMemorySize"], Environment.NewLine);
                    result.Write("TotalVirtualRam: {0:N0}kB{1}", item["TotalVirtualMemorySize"], Environment.NewLine);
                    result.Write("FreeRam: {0:N0}kB{1}", item["FreePhysicalMemory"], Environment.NewLine);
                    result.Write("FreeVirtualRam: {0:N0}kB{1}", item["FreeVirtualMemory"], Environment.NewLine);
                    result.Write("Locale: {0}{1}", item["Locale"], Environment.NewLine);
                    result.Write("Language: {0}{1}", item["OSLanguage"], Environment.NewLine);
                    result.Write("CountryCode: {0}{1}", item["CountryCode"], Environment.NewLine);
                    result.Write("Architecture: {0}{1}", item["OSArchitecture"], Environment.NewLine);
                }
                result.Write("Network: {0}{1}", System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() ? "Active" : "Inactive", Environment.NewLine);
                result.Write("NetFrameworkVersion: {0}{1}", Environment.Version, Environment.NewLine);
                result.Write("XnaFrameworkVersion: {0}{1}", ((AssemblyFileVersionAttribute)typeof(Game).Assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false).GetValue(0)).Version, Environment.NewLine);
                result.WriteLine();
                result.WriteLine("PROCESS");
                result.Write("BabyBashExe: {0}{1}", entryAssembly.CodeBase, Environment.NewLine);
                result.Write("BabyBashAssembly: {0}{1}", entryAssembly.FullName, Environment.NewLine);
                result.Write("Architecture: {0}{1}", Environment.Is64BitProcess ? "64 bit" : "32 bit", Environment.NewLine);
                result.Write("WorkingSet: {0:N0}kB{1}", proc.WorkingSet64  / 1024, Environment.NewLine);
                result.Write("PrivateBytes: {0:N0}kB{1}", proc.PrivateMemorySize64 / 1024, Environment.NewLine);
                result.Write("VirtualSize: {0:N0}kB{1}", proc.VirtualMemorySize64 / 1024, Environment.NewLine);
                result.Write("PeakWorkingSet: {0:N0}kB{1}", proc.PeakWorkingSet64 / 1024, Environment.NewLine);
                result.Write("PeakVirtualSize: {0:N0}kB{1}", proc.PeakVirtualMemorySize64 / 1024, Environment.NewLine);
                result.Write("WallRuntime: {0:c}{1}", DateTime.Now.Subtract(proc.StartTime), Environment.NewLine);
                result.Write("CpuTime: {0:c}{1}", proc.TotalProcessorTime, Environment.NewLine);
                result.WriteLine();
                result.WriteLine("ASSEMBLIES");
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                    result.WriteLine(a.FullName);
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    // This isn't available on Win XP.
                    result.WriteLine();
                    result.WriteLine("EXPERIENCE INDEX");       
                    using (var item = experienceInfo.Get().Cast<ManagementBaseObject>().First())
                    {
                        result.Write("AssesmentState: {0}{1}", this.WinSATAssessmentStateToString(item["WinSATAssessmentState"].ToString()), Environment.NewLine);
                        result.Write("CPUScore: {0}{1}", item["CPUScore"], Environment.NewLine);
                        result.Write("D3DScore: {0}{1}", item["D3DScore"], Environment.NewLine);
                        result.Write("DiskScore: {0}{1}", item["DiskScore"], Environment.NewLine);
                        result.Write("GraphicsScore: {0}{1}", item["GraphicsScore"], Environment.NewLine);
                        result.Write("MemoryScore: {0}{1}", item["MemoryScore"], Environment.NewLine);
                    }
                }
                result.WriteLine();
                result.WriteLine("CPU");
                var cpus = procInfo.Get();
                result.Write("CpuCount: {0}{1}", cpus.Count, Environment.NewLine);
                foreach (var item in cpus)
                {
                    result.Write("{0}: {1} {2}{3}", item["DeviceID"], item["Name"], item["Description"], Environment.NewLine);
                    result.Write("{0}: PhysicalCores = {1} LogicalCores = {2}{3}", item["DeviceID"], item["NumberOfCores"], item["NumberOfLogicalProcessors"], Environment.NewLine);
                }
                result.WriteLine();
                result.WriteLine("MONITOR AND GRAPHICS");
                foreach (var item in videoController.Get())
                {
                    result.Write("VideoCard: {0}{1}", item["Description"], Environment.NewLine);
                    result.Write("DisplayMode: {0}x{1}x{2}bpp{3}", item["CurrentHorizontalResolution"], item["CurrentVerticalResolution"], item["CurrentBitsPerPixel"], Environment.NewLine);
                    result.Write("Ram: {0:N0}kB{1}", (uint)item["AdapterRAM"]/1024, Environment.NewLine);
                    result.Write("DriverVersion: {0}{1}", item["DriverVersion"], Environment.NewLine);
                    result.Write("Availability: {0}{1}", this.Win32VideoAvailabilityToString((UInt16)item["Availability"]), Environment.NewLine);
                }
                result.Write("MonitorCount: {0}{1}", screens.Length, Environment.NewLine);
                for (int i = 0; i < screens.Length; i++)
                    result.Write("Monitor{0}: '{1}' {2}x{3}x{4}bpp {5}{6}", i, screens[i].DeviceName, screens[i].Bounds.Width, screens[i].Bounds.Height, screens[i].BitsPerPixel, screens[i].Primary ? "Primary" : String.Empty, Environment.NewLine);
                result.WriteLine();
                result.WriteLine("SOUND");
                foreach (var item in soundInfo.Get())
                    result.Write("SoundDevice: {0}{1}", item["Name"], Environment.NewLine);
                result.WriteLine();
                result.WriteLine("POWER");
                result.Write("Power: {0}{1}", System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus, Environment.NewLine);
                result.Write("Battery: {0:P0}{1}", System.Windows.Forms.SystemInformation.PowerStatus.BatteryLifePercent, Environment.NewLine);
                result.WriteLine();
                result.WriteLine("DISK");
                result.Write("SystemDrive: {0}{1}", Environment.GetEnvironmentVariable("SystemDrive"), Environment.NewLine);
                result.Write("SystemDriveFreeSpace: {0:N2}MB{1}", (double)drives.First(d => d.Name.StartsWith(Environment.GetEnvironmentVariable("SystemDrive"), StringComparison.CurrentCultureIgnoreCase)).AvailableFreeSpace / (1024 * 1024), Environment.NewLine);
                result.Write("TempDriveFreeSpace: {0:N2}MB{1}", (double)drives.First(d => d.Name.StartsWith(Environment.GetEnvironmentVariable("SystemDrive"), StringComparison.CurrentCultureIgnoreCase)).AvailableFreeSpace / (1024 * 1024), Environment.NewLine);
                result.Write("BabyBashInstallDriveFreeSpace: {0:N2}MB{1}", (double)drives.First(d => d.Name.StartsWith(Path.GetPathRoot(Environment.GetEnvironmentVariable("TEMP")), StringComparison.CurrentCultureIgnoreCase)).AvailableFreeSpace / (1024 * 1024), Environment.NewLine);
                result.Flush();
            }
        }

        public String CreateComputerDetails()
        {
            using (var result = new MemoryStream())
            {
                var writer = new StreamWriter(result, Encoding.UTF8);
                this.CreateComputerDetails(writer);

                result.Seek(0, SeekOrigin.Begin);
                
                var reader = new StreamReader(result, Encoding.UTF8);
                return reader.ReadToEnd();
            }
        }

        private String Win32VideoAvailabilityToString(int availability)
        {
            switch (availability)
            {
                case 1:
                    return "Other";
                case 2:
                    return "Unknown";
                case 3:
                    return "Running or Full Power";
                case 4:
                    return "Warning";
                case 5:
                    return "In Test";
                case 6:
                    return "Not Applicable";
                case 7:
                    return "Power Off";
                case 8:
                    return "Offline";
                case 9:
                    return "Off Duty";
                case 10:
                    return "Degraded";
                case 11:
                    return "Not Installed";
                case 12:
                    return "Install Error";
                case 13:
                    return "Power Save - Unknown";
                case 14:
                    return "Power Save - Low Power Mode";
                case 15:
                    return "Power Save - Standby";
                case 16:
                    return "Power Cycle";
                case 17:
                    return "Power Save - Warning";
                default:
                    return availability.ToString();
            }
        }
        private String WinSATAssessmentStateToString(string state)
        {
            int s;
            if (!Int32.TryParse(state, out s))
                return state;
            
            switch (s)
            {
                case 0:
                    return "StateUnknown";
                case 1:
                    return "Valid";
                case 2:
                    return "IncoherentWithHardware";
                case 3:
                    return "NoAssessmentAvailable";
                case 4:
                    return "Invalid";
                default:
                    return state.ToString();
            }
        }
    }
}
