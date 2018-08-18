using System;

namespace MemoQ.VideoPreview
{
    public class Log
    {
        public static string PreviewUnavailableMessage = "Preview service of memoQ is unavailable.";
        
        #region Singleton
        private static readonly Lazy<Log> lazyInstance = new Lazy<Log>(() => new Log());

        public static Log Instance { get { return lazyInstance.Value; } }

        private Log()
        {
        }
        #endregion Singleton

        public event EventHandler<MessageAddedEventArgs> MessageAdded;

        protected virtual void OnMessageAdded(MessageAddedEventArgs e) => MessageAdded?.Invoke(this, e);

        public void WriteMessage(string message, LogEntry.SeverityOption severity, string origin = "Application")
        {
            var logEntry = new LogEntry(DateTime.Now, origin, severity, message);
            OnMessageAdded(new MessageAddedEventArgs(logEntry));
        }

        public class MessageAddedEventArgs : EventArgs
        {
            public MessageAddedEventArgs(LogEntry logEntry)
            {
                LogEntry = logEntry ?? throw new ArgumentNullException(nameof(logEntry));
            }

            public LogEntry LogEntry { get; }
        }

        public class LogEntry
        {
            public enum SeverityOption
            {
                Error,
                Warning,
                Info,
                Verbose,
            }

            public LogEntry(DateTime time, string origin, SeverityOption severity, string message)
            {
                Time = time;
                Origin = origin ?? throw new ArgumentNullException(nameof(origin));
                Severity = severity;
                Message = message ?? throw new ArgumentNullException(nameof(message));
            }

            public DateTime Time { get; }
            public string Origin { get; }
            public SeverityOption Severity { get; }
            public string Message { get; }
        }
    }
}
