using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Timers;
using Timer = System.Timers.Timer;

namespace HoursCounterGUI
{
    public partial class Form : System.Windows.Forms.Form
    {
        private List<string> detailedApps;
        private Dictionary<string, int> appsTime;

        public string filesDirectory = Directory.GetCurrentDirectory() + @"\Files\";

        private const string SERVICE_NAME = "Hours Counter";
        ServiceController serviceController;

        Timer updater;

        public Form()
        {
            InitializeComponent();
        }

        private void OnStart(object sender, EventArgs e)
        {
            FileStream GUIPointer = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\HCGUI", FileMode.OpenOrCreate);
            using (StreamWriter file = new StreamWriter(GUIPointer))
            {
                file.Write(filesDirectory);
            }

            detailedApps = File.ReadAllLines(filesDirectory + "Apps.data").ToList();

            foreach (string app in detailedApps)
            {
                appsList.Items.Add(Path.GetFileName(app));
            }

            appsTime = new Dictionary<string, int>(detailedApps.Count);

            string[] timeRecords = File.ReadAllLines(filesDirectory + "Time.data");

            foreach (string record in timeRecords)
            {
                appsTime.Add(
                             record.Substring(0, record.IndexOf(": ")),
                   int.Parse(record.Remove(0, record.IndexOf(": ") + 1)));
            }

            serviceController = new ServiceController(SERVICE_NAME);

            updater = new Timer();
            updater.Enabled = true;
            updater.Interval = 1000d;
            updater.Elapsed += Update;
        }

        private void Update(object sender, ElapsedEventArgs e)
        {
            CheckService();

            string[] upToDateTimes = File.ReadAllLines(filesDirectory + "Time.data");

            if (upToDateTimes.Length == 0)
                return;

            for (int i = 0; i < detailedApps.Count; i++)
            {
                int timeValue = int.Parse(upToDateTimes[i].Remove(0, upToDateTimes[i].IndexOf(": ") + 1));

                if (appsTime.ContainsKey(detailedApps[i]))
                {
                    appsTime[detailedApps[i]] = timeValue;
                }
                else
                {
                    appsTime.Add(detailedApps[i], timeValue);
                }
            }

            if (InvokeIndex() != -1)
            {
                time.Invoke((Action)(() => time.Text = ConvertTime(appsTime[detailedApps[InvokeIndex()]])));
            }
        }

        private int InvokeIndex()
        {
            if (appsList.InvokeRequired)
            {
                return (int)appsList.Invoke(
                    new Func<int>(() => InvokeIndex()));
            }
            else
            {
                return appsList.SelectedIndex;
            }
        }

        private void AppSelected(object sender, EventArgs e)
        {
            if (appsList.SelectedIndex == -1)
                return;

            removeButton.Enabled = true;

            time.Text = "0h 0m 0s";

            name.Text = FileVersionInfo.GetVersionInfo(detailedApps[appsList.SelectedIndex]).ProductName;

            if (!appsTime.ContainsKey(detailedApps[appsList.SelectedIndex]))
                return;

            time.Text = ConvertTime(appsTime[detailedApps[appsList.SelectedIndex]]);
        }

        private string ConvertTime(int rawSeconds)
        {
            int hours = rawSeconds / 3600;
            int minutes = rawSeconds % 3600 / 60;
            int seconds = rawSeconds % 3600 % 60;

            return $"{hours}h {minutes}m {seconds}s";
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            appAdd.ShowDialog();

            foreach (string file in appAdd.FileNames)
            {
                detailedApps.Add(file);

                File.WriteAllText(filesDirectory + "Apps.data", file);

                appsList.Items.Add(Path.GetFileName(file));
            }
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            removeButton.Enabled = false;

            name.Text = "<select app>";
            time.Text = "<select app>";

            int selectedIndex = appsList.SelectedIndex;
            appsList.ClearSelected();

            appsList.Items.RemoveAt(selectedIndex);

            detailedApps.RemoveAt(selectedIndex);

            File.WriteAllLines(filesDirectory + "Apps.data", detailedApps);
        }

        private void CheckService()
        {
            switch (serviceController.Status)
            {
                case ServiceControllerStatus.Running:
                    serviceStatus.Text = "Running";
                    break;

                case ServiceControllerStatus.Stopped:
                    serviceStatus.Text = "Stopped";
                    break;

                case ServiceControllerStatus.Paused:
                    serviceStatus.Text = "Paused";
                    break;

                case ServiceControllerStatus.StopPending:
                    serviceStatus.Text = "Stopping";
                    break;

                case ServiceControllerStatus.StartPending:
                    serviceStatus.Text = "Starting";
                    break;

                default:
                    serviceStatus.Text = "Status Changing";
                    break;
            }

            serviceController.Refresh();
        }

        private void Start_Click(object sender, EventArgs e)
        {
            serviceController.Start();
        }

        private void Stop_Click(object sender, EventArgs e)
        {
            serviceController.Stop();
        }

        private void Restart_Click(object sender, EventArgs e)
        {
            serviceController.Stop();

            serviceController.WaitForStatus(ServiceControllerStatus.Stopped);

            serviceController.Start();
        }
    }
}
