using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SGEHealthCheckService
{
    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public int dwServiceType;
        public ServiceState dwCurrentState;
        public int dwControlsAccepted;
        public int dwWin32ExitCode;
        public int dwServiceSpecificExitCode;
        public int dwCheckPoint;
        public int dwWaitHint;
    };

    public class ServerStats
    {
        public string MachineName { get; set; }
        public float Cpu { get; set; }
        public float HardDrive { get; set; }
        public float Memory { get; set; }

    }

    partial class HealthCheck : ServiceBase
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        public HealthCheck()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            // Set up a timer that triggers every minute.
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 240010; // 2 minutes
            timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
        }
        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            String filename = "C:\\Temp\\servers.txt";
            String secondValue;
            String diskUsage;
            String memUsage;
            String cpuUsage;
            float cpuValue;
            try
            {
                var machineNames = File.ReadAllLines(filename);

                //Add some error handling in case file is not in place.
                foreach (string machineName in machineNames)
                {

                    Task t = Task.Run(() =>
                    {
                        try
                        {
                            PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", machineName);
                            PerformanceCounter diskcounter = new PerformanceCounter("LogicalDisk", "% Free Space", "C:", machineName);
                            PerformanceCounter memoryCounter = new PerformanceCounter("Memory", "Available MBytes", "", machineName);
                            cpuUsage = "Average CPU utilization is: " + cpuCounter.NextValue() + "%";

                            System.Threading.Thread.Sleep(1000);
                            // now matches task manager reading
                            cpuValue = cpuCounter.NextValue();
                            secondValue = "Average CPU utilization is: " + cpuValue + "%";
                            diskUsage = "Current space available on C: drive is: " + diskcounter.NextValue() + "%";
                            memUsage = "Current available memory is: " + memoryCounter.NextValue() + " megabytes";

                            
                            List<ServerStats> _serverStats = new List<ServerStats>();
                            _serverStats.Add(new ServerStats()
                            {
                                MachineName = machineName,
                                Cpu = cpuValue,
                                HardDrive = diskcounter.NextValue(),
                                Memory = memoryCounter.NextValue()
                            });

                            string json = JsonConvert.SerializeObject(_serverStats.ToArray());

                            //write string to file
                            string fileName = "C:\\Temp\\" + machineName + ".json";
                            System.IO.File.WriteAllText(fileName, json);
                        }
                        catch (Exception )
                        {
                            //Unable to access specified machine.  
                            Console.WriteLine("Unable to access machine named: " + machineName);

                            List<ServerStats> _serverStats = new List<ServerStats>();
                            _serverStats.Add(new ServerStats()
                            {
                                MachineName = machineName,
                                Cpu = 0.0F,
                                HardDrive = 0.0F,
                                Memory = 0.0F
                            });

                            string json = JsonConvert.SerializeObject(_serverStats.ToArray());

                            //write string to file
                            string fileName = "C:\\Temp\\" + machineName + ".json";
                            System.IO.File.WriteAllText(fileName, json);
                        }
                    });

                }
                Console.ReadLine();

            }
            catch (Exception )
            {
                //Add error handling as necessary
            }

        }
    }
}
