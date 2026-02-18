using System;
using System.Diagnostics;
using System.IO;

namespace FolderMonitorService
{
    /// <summary>
    /// Custom TraceListener that routes all Trace output through our Logger system
    /// This ensures all logging (including Trace calls) uses timestamps and 2MB rotation
    /// </summary>
    public class LoggerTraceListener : TraceListener
    {
        private readonly Logger _logger;
        private readonly LogLevel _defaultLevel;

        public LoggerTraceListener(Logger logger, LogLevel defaultLevel = LogLevel.Info)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultLevel = defaultLevel;
        }

        public override void Write(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                _logger.Log(_defaultLevel, message.TrimEnd('\r', '\n'));
            }
        }

        public override void WriteLine(string message)
        {
            Write(message);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            var logLevel = ConvertTraceEventTypeToLogLevel(eventType);
            var formattedMessage = $"[{source}] {message}";
            _logger.Log(logLevel, formattedMessage);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            var logLevel = ConvertTraceEventTypeToLogLevel(eventType);
            var message = string.Format(format, args);
            var formattedMessage = $"[{source}] {message}";
            _logger.Log(logLevel, formattedMessage);
        }

        private LogLevel ConvertTraceEventTypeToLogLevel(TraceEventType eventType)
        {
            switch (eventType)
            {
                case TraceEventType.Critical:
                    return LogLevel.Critical;
                case TraceEventType.Error:
                    return LogLevel.Error;
                case TraceEventType.Warning:
                    return LogLevel.Warning;
                case TraceEventType.Information:
                    return LogLevel.Info;
                case TraceEventType.Verbose:
                    return LogLevel.Debug;
                default:
                    return LogLevel.Info;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logger?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}