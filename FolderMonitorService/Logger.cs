using System;
using System.IO;
using System.Text;

namespace FolderMonitorService
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public class Logger : IDisposable
    {
        private readonly string _logFilePath;
        private readonly long _maxFileSizeBytes;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        // Default constructor with 2 MB limit
        public Logger() : this(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FolderMonitorService.log"), 2 * 1024 * 1024)
        {
        }

        // Constructor with custom path and size limit
        public Logger(string logFilePath, long maxFileSizeBytes = 2 * 1024 * 1024)
        {
            _logFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));
            _maxFileSizeBytes = maxFileSizeBytes;

            // Ensure log directory exists
            var logDirectory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        public void LogDebug(string message) => Log(LogLevel.Debug, message);
        public void LogInfo(string message) => Log(LogLevel.Info, message);
        public void LogWarning(string message) => Log(LogLevel.Warning, message);
        public void LogError(string message) => Log(LogLevel.Error, message);
        public void LogError(string message, Exception exception) => Log(LogLevel.Error, $"{message} | Exception: {exception}");
        public void LogCritical(string message) => Log(LogLevel.Critical, message);
        public void LogCritical(string message, Exception exception) => Log(LogLevel.Critical, $"{message} | Exception: {exception}");

        public void Log(LogLevel level, string message)
        {
            if (_disposed)
                return;

            if (string.IsNullOrWhiteSpace(message))
                return;

            lock (_lockObject)
            {
                try
                {
                    // Check if log rotation is needed
                    if (File.Exists(_logFilePath) && new FileInfo(_logFilePath).Length >= _maxFileSizeBytes)
                    {
                        RotateLogFile();
                    }

                    // Create log entry with timestamp
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logEntry = $"[{timestamp}] [{level.ToString().ToUpper()}] {message}{Environment.NewLine}";

                    // Write to log file
                    File.AppendAllText(_logFilePath, logEntry, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    // Fallback to console if file logging fails
                    Console.WriteLine($"[LOGGER ERROR] Failed to write to log file: {ex.Message}");
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level.ToString().ToUpper()}] {message}");
                }
            }
        }

        private void RotateLogFile()
        {
            try
            {
                var backupPath = GetBackupLogPath();
                
                // Delete old backup if it exists
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                // Move current log to backup
                File.Move(_logFilePath, backupPath);

                // Log rotation message in new file
                var rotationMessage = $"Log rotated at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - Previous log saved as {Path.GetFileName(backupPath)}";
                Log(LogLevel.Info, rotationMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGGER ERROR] Failed to rotate log file: {ex.Message}");
            }
        }

        private string GetBackupLogPath()
        {
            var directory = Path.GetDirectoryName(_logFilePath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(_logFilePath);
            var extension = Path.GetExtension(_logFilePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            return Path.Combine(directory, $"{fileNameWithoutExt}_{timestamp}{extension}");
        }

        public long GetCurrentLogFileSize()
        {
            if (File.Exists(_logFilePath))
            {
                return new FileInfo(_logFilePath).Length;
            }
            return 0;
        }

        public string GetLogFilePath()
        {
            return _logFilePath;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Log shutdown message
                Log(LogLevel.Info, "Logger is being disposed");
                _disposed = true;
            }
        }
    }

    // Static logger instance for easy access throughout the application
    public static class AppLogger
    {
        private static readonly Lazy<Logger> _logger = new Lazy<Logger>(() => new Logger());

        public static Logger Instance => _logger.Value;

        public static void LogDebug(string message) => Instance.LogDebug(message);
        public static void LogInfo(string message) => Instance.LogInfo(message);
        public static void LogWarning(string message) => Instance.LogWarning(message);
        public static void LogError(string message) => Instance.LogError(message);
        public static void LogError(string message, Exception exception) => Instance.LogError(message, exception);
        public static void LogCritical(string message) => Instance.LogCritical(message);
        public static void LogCritical(string message, Exception exception) => Instance.LogCritical(message, exception);
    }
}