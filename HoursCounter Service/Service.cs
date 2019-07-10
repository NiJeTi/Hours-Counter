using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Timers;

namespace HoursCounterService
{
    public partial class Service : ServiceBase
    {
        public string filesDirectory;

        private Dictionary<string, int> appsTime;

        private Timer timer;
        private int tickCount;

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            filesDirectory = File.ReadAllText(Directory.GetCurrentDirectory() + @"\HCGUI");

            appsTime = new Dictionary<string, int>(0);

            string[] timeRecords = File.ReadAllLines(filesDirectory + "Time.data");

            foreach (string record in timeRecords)
            {
                appsTime.Add(record.Substring(0, record.IndexOf(": ")),
                   int.Parse(record.Remove(0, record.IndexOf(": ") + 1)));
            }

            timer = new Timer();
            timer.Enabled = true;
            timer.Interval = 1000d;
            timer.Elapsed += Tick;
        }

        private void Tick(object sender, ElapsedEventArgs e)
        {
            List<Process> processes = new List<Process>();
            tickCount++;

            string[] appsToCount = File.ReadAllLines(filesDirectory + "Apps.data");

            if (appsTime.Count > appsToCount.Length)
            {
                Dictionary<string, int> buffer = new Dictionary<string, int>(appsToCount.Length);

                foreach (string app in appsToCount)
                {
                    if (appsTime.ContainsKey(app))
                    {
                        buffer.Add(app, appsTime[app]);
                    }
                }

                appsTime.Clear();
                File.WriteAllText(filesDirectory + "Time.data", string.Empty);

                appsTime = buffer;
            }

            foreach (string app in appsToCount)
            {
                if (!appsTime.ContainsKey(app))
                {
                    appsTime.Add(app, 0);
                }

                string fileName = Path.GetFileName(app);
                string processName = fileName.Remove(fileName.Length - 4, 4);

                Process[] proc = Process.GetProcessesByName(processName);

                if (proc.Length != 0)
                {
                    processes.Add(proc[0]);
                }
            }

            if (processes.Count != 0)
            {
                foreach (Process proc in processes)
                {
                    if (!proc.HasExited)
                    {
                        appsTime[proc.MainModule.FileName]++;
                    }
                }
            }

            if (tickCount == 5)
            {
                string writeData = string.Join("\n", appsTime.Select(elem => $"{elem.Key}: {elem.Value}").ToArray());

                File.WriteAllText(filesDirectory + "Time.data", writeData);

                tickCount = 0;
            }
        }

        protected override void OnStop()
        {

        }
    }
}
