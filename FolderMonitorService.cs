using System;
using System.IO;
using System.ServiceProcess;
using System.Timers;
using System.Net;
using System.Net.Mail;
using System.Configuration;
using System.Diagnostics;

namespace FolderMonitorService
{
    public partial class FolderMonitorService : ServiceBase
    {
        private FileSystemWatcher watcher;
        private Timer checkTimer;
        private string folderPath;
        private DateTime lastFileTime;
        private double alertIntervalMinutes;
        private TimeSpan monitorStart;
        private TimeSpan monitorEnd;

        public FolderMonitorService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Trace.TraceInformation("Service is starting.");

            folderPath = ConfigurationManager.AppSettings["MonitorFolder"];
            alertIntervalMinutes = double.Parse(ConfigurationManager.AppSettings["AlertIntervalMinutes"]);
            monitorStart = TimeSpan.Parse(ConfigurationManager.AppSettings["MonitorStartTime"]);
            monitorEnd = TimeSpan.Parse(ConfigurationManager.AppSettings["MonitorEndTime"]);

            lastFileTime = DateTime.Now;
            watcher = new FileSystemWatcher(folderPath);
            watcher.Created += OnFileCreated;
            watcher.EnableRaisingEvents = true;

            checkTimer = new Timer(60000); // kontroll varje minut
            checkTimer.Elapsed += CheckFolder;
            checkTimer.Start();

            Trace.TraceInformation("Service started successfully.");
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            Trace.TraceInformation($"File created: {e.FullPath}");
            lastFileTime = DateTime.Now;
        }

        private void CheckFolder(object sender, ElapsedEventArgs e)
        {
            var now = DateTime.Now.TimeOfDay;
            if (now < monitorStart || now > monitorEnd)
                return; // Gör ingenting utanför övervakningstid

            if ((DateTime.Now - lastFileTime).TotalMinutes > alertIntervalMinutes)
            {
                Trace.TraceWarning("No new files detected within the alert interval.");
                SendAlertEmail();
                lastFileTime = DateTime.Now;
            }
        }

        private string DecodeBase64(string encodedValue)
        {
            var base64EncodedBytes = Convert.FromBase64String(encodedValue);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        private string EncodeBase64(string plainValue)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainValue);
            return Convert.ToBase64String(plainTextBytes);
        }

        private void SendAlertEmail()
        {
            try
            {
                Trace.TraceInformation("Sending alert email.");

                var smtpClient = new SmtpClient(ConfigurationManager.AppSettings["SmtpServer"])
                {
                    Port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]),
                    Credentials = new NetworkCredential(
                        ConfigurationManager.AppSettings["SmtpUser"],
                        DecodeBase64(ConfigurationManager.AppSettings["SmtpPassword"])
                    ),
                    EnableSsl = bool.Parse(ConfigurationManager.AppSettings["EnableSsl"]),
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(ConfigurationManager.AppSettings["MailFrom"]),
                    Subject = ConfigurationManager.AppSettings["MailSubject"],
                    Body = $"Inga nya filer har kommit in i mappen {folderPath} under de senaste {alertIntervalMinutes} minuterna.",
                    IsBodyHtml = false,
                };
                mailMessage.To.Add(ConfigurationManager.AppSettings["MailTo"]);

                smtpClient.Send(mailMessage);
                Trace.TraceInformation("Alert email sent successfully.");
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to send alert email: {ex.Message}");
            }
        }

        protected override void OnStop()
        {
            Trace.TraceInformation("Service is stopping.");

            watcher.Dispose();
            checkTimer.Stop();

            Trace.TraceInformation("Service stopped successfully.");
        }
    }
}
