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
        private Logger logger;

        public FolderMonitorService()
        {
            InitializeComponent();
            logger = new Logger();

            // Set up TraceListener to route all Trace calls through our Logger
            SetupTraceLogging();
        }

        private void SetupTraceLogging()
        {
            try
            {
                // Check if trace integration is enabled
                var enableTrace = System.Configuration.ConfigurationManager.AppSettings["EnableTraceIntegration"];
                if (string.Equals(enableTrace, "false", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInfo("Trace integration disabled via configuration");
                    return;
                }

                // Remove default listeners to prevent duplicate output
                Trace.Listeners.Clear();

                // Add our custom TraceListener that routes to Logger
                var traceListener = new LoggerTraceListener(logger, LogLevel.Info);
                Trace.Listeners.Add(traceListener);

                // Configure trace settings
                Trace.AutoFlush = true;

                logger.LogInfo($"TraceListener configured - all Trace calls will route through Logger with timestamps and 2MB rotation");
                logger.LogInfo($"Log file: {logger.GetLogFilePath()}");
                logger.LogInfo($"Current log size: {FormatFileSize(logger.GetCurrentLogFileSize())}");
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to setup trace logging", ex);
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} bytes";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024:F1} KB";
            else
                return $"{bytes / (1024 * 1024):F1} MB";
        }

        protected override void OnStart(string[] args)
        {
            logger.LogInfo("=== FolderMonitorService is starting ===");
            AppLogger.LogInfo("Service startup initiated by Windows Service Manager");

            try
            {
                // Load configuration with logging
                folderPath = ConfigurationManager.AppSettings["MonitorFolder"];
                logger.LogInfo($"Monitor folder configured: {folderPath}");

                alertIntervalMinutes = double.Parse(ConfigurationManager.AppSettings["AlertIntervalMinutes"]);
                logger.LogInfo($"Alert interval configured: {alertIntervalMinutes} minutes");

                monitorStart = TimeSpan.Parse(ConfigurationManager.AppSettings["MonitorStartTime"]);
                monitorEnd = TimeSpan.Parse(ConfigurationManager.AppSettings["MonitorEndTime"]);
                logger.LogInfo($"Monitor schedule: {monitorStart:hh\\:mm} - {monitorEnd:hh\\:mm}");

                // Validate folder path
                if (!Directory.Exists(folderPath))
                {
                    logger.LogWarning($"Monitor folder does not exist: {folderPath}. Creating directory...");
                    Directory.CreateDirectory(folderPath);
                    logger.LogInfo($"Monitor folder created successfully: {folderPath}");
                }

                // Initialize file monitoring
                lastFileTime = DateTime.Now;
                watcher = new FileSystemWatcher(folderPath);
                watcher.Created += OnFileCreated;
                watcher.Changed += OnFileChanged;
                watcher.Deleted += OnFileDeleted;
                watcher.Error += OnWatcherError;
                watcher.EnableRaisingEvents = true;

                logger.LogInfo("FileSystemWatcher initialized and monitoring started");

                // Initialize timer for periodic checks
                checkTimer = new Timer(60000); // Check every minute
                checkTimer.Elapsed += CheckFolder;
                checkTimer.Start();

                logger.LogInfo("Check timer initialized (60 second interval)");
                logger.LogInfo("=== FolderMonitorService started successfully ===");

                // Demonstrate unified logging - both Logger and Trace calls now use the same system
                logger.LogInfo("Direct Logger call: Service startup completed");
                Trace.TraceInformation("Trace call: Service started successfully - routed through Logger");

                // Log current configuration for debugging
                logger.LogInfo($"Configuration summary:");
                logger.LogInfo($"  - Monitor folder: {folderPath}");
                logger.LogInfo($"  - Alert interval: {alertIntervalMinutes} minutes");
                logger.LogInfo($"  - Monitor hours: {monitorStart:hh\\:mm} to {monitorEnd:hh\\:mm}");
                logger.LogInfo($"  - Log file: {logger.GetLogFilePath()}");
                logger.LogInfo($"  - Max log size: {FormatFileSize(2 * 1024 * 1024)} (with auto-rotation)");
            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to start FolderMonitorService", ex);
                throw;
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            logger.LogInfo($"File CREATED: {e.FullPath} (Size: {GetFileSize(e.FullPath)})");
            lastFileTime = DateTime.Now;

            // Trace calls now automatically route through Logger
            Trace.TraceInformation($"File created: {e.FullPath}");
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            logger.LogDebug($"File CHANGED: {e.FullPath} (Size: {GetFileSize(e.FullPath)})");
            lastFileTime = DateTime.Now;
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            logger.LogWarning($"File DELETED: {e.FullPath}");
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            logger.LogError($"FileSystemWatcher error occurred", e.GetException());

            // Try to restart the watcher
            try
            {
                logger.LogInfo("Attempting to restart FileSystemWatcher...");
                watcher.EnableRaisingEvents = false;
                watcher.EnableRaisingEvents = true;
                logger.LogInfo("FileSystemWatcher restarted successfully");
            }
            catch (Exception restartEx)
            {
                logger.LogCritical("Failed to restart FileSystemWatcher", restartEx);
            }
        }

        private string GetFileSize(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length < 1024)
                    return $"{fileInfo.Length} bytes";
                else if (fileInfo.Length < 1024 * 1024)
                    return $"{fileInfo.Length / 1024:F1} KB";
                else
                    return $"{fileInfo.Length / (1024 * 1024):F1} MB";
            }
            catch
            {
                return "unknown size";
            }
        }

        private void CheckFolder(object sender, ElapsedEventArgs e)
        {
            var now = DateTime.Now.TimeOfDay;

            // Check if we're within monitoring hours
            if (now < monitorStart || now > monitorEnd)
            {
                logger.LogDebug($"Outside monitoring hours ({now:hh\\:mm}). No check performed.");
                return;
            }

            var timeSinceLastFile = DateTime.Now - lastFileTime;
            logger.LogDebug($"Periodic check: Last file activity was {timeSinceLastFile.TotalMinutes:F1} minutes ago");

            if (timeSinceLastFile.TotalMinutes > alertIntervalMinutes)
            {
                logger.LogWarning($"ALERT: No new files detected for {timeSinceLastFile.TotalMinutes:F1} minutes (threshold: {alertIntervalMinutes} minutes)");
                SendAlertEmail();
                lastFileTime = DateTime.Now; // Reset to avoid spam
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
            logger.LogInfo("=== Sending alert email ===");

            try
            {
                // Log SMTP configuration (without sensitive data)
                var smtpServer = ConfigurationManager.AppSettings["SmtpServer"];
                var smtpPort = ConfigurationManager.AppSettings["SmtpPort"];
                var smtpUser = ConfigurationManager.AppSettings["SmtpUser"];
                var mailFrom = ConfigurationManager.AppSettings["MailFrom"];
                var mailTo = ConfigurationManager.AppSettings["MailTo"];

                logger.LogInfo($"SMTP Server: {smtpServer}:{smtpPort}");
                logger.LogInfo($"From: {mailFrom} -> To: {mailTo}");
                logger.LogInfo($"User: {smtpUser}");

                var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = int.Parse(smtpPort),
                    Credentials = new NetworkCredential(
                        smtpUser,
                        DecodeBase64(ConfigurationManager.AppSettings["SmtpPassword"])
                    ),
                    EnableSsl = bool.Parse(ConfigurationManager.AppSettings["EnableSsl"]),
                };

                var currentTime = DateTime.Now;
                var timeSinceLastFile = currentTime - lastFileTime;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(mailFrom),
                    Subject = $"[ALERT] {ConfigurationManager.AppSettings["MailSubject"]} - {currentTime:yyyy-MM-dd HH:mm}",
                    Body = $@"FOLDER MONITORING ALERT

Time: {currentTime:yyyy-MM-dd HH:mm:ss}
Folder: {folderPath}
Alert Threshold: {alertIntervalMinutes} minutes
Time Since Last File: {timeSinceLastFile.TotalMinutes:F1} minutes
Last File Activity: {lastFileTime:yyyy-MM-dd HH:mm:ss}

No new files have been detected in the monitored folder within the configured alert interval.

This is an automated message from FolderMonitorService.
Server: {Environment.MachineName}
Service Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}",
                    IsBodyHtml = false,
                };
                mailMessage.To.Add(mailTo);

                logger.LogInfo($"Sending email with subject: {mailMessage.Subject}");
                smtpClient.Send(mailMessage);

                logger.LogInfo("Alert email sent successfully");

                // Trace integration - this will also go through Logger now
                Trace.TraceInformation("Alert email sent successfully - via integrated trace logging");
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to send alert email", ex);
                // Trace errors also route through Logger now
                Trace.TraceError($"Failed to send alert email: {ex.Message}");
            }
        }

        protected override void OnStop()
        {
            logger.LogInfo("=== FolderMonitorService is stopping ===");

            try
            {
                if (watcher != null)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                    logger.LogInfo("FileSystemWatcher disposed");
                }

                if (checkTimer != null)
                {
                    checkTimer.Stop();
                    checkTimer.Dispose();
                    logger.LogInfo("Check timer stopped and disposed");
                }

                logger.LogInfo("=== FolderMonitorService stopped successfully ===");

                // Final trace message - will go through Logger
                Trace.TraceInformation("Service stopped successfully - all logging unified");
            }
            catch (Exception ex)
            {
                logger.LogError("Error occurred during service shutdown", ex);
                Trace.TraceError($"Error during service shutdown: {ex.Message}");
            }
            finally
            {
                // Clean up trace listeners before disposing logger
                try
                {
                    Trace.Listeners.Clear();
                }
                catch { /* Ignore cleanup errors */ }

                // Dispose logger last
                logger?.Dispose();
            }
        }
    }
}
