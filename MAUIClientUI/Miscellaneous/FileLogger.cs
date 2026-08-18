using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace MAUIClientUI.Miscellaneous
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _filePath;
        private readonly object _lock = new object();

        public FileLoggerProvider(string filePath)
        {
            _filePath = filePath;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(_filePath, categoryName, _lock);
        }

        public void Dispose()
        {
        }
    }

    public class FileLogger : ILogger
    {
        private readonly string _filePath;
        private readonly string _categoryName;
        private readonly object _lock;

        public FileLogger(string filePath, string categoryName, object lockObj)
        {
            _filePath = filePath;
            _categoryName = categoryName;
            _lock = lockObj;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            string message = formatter(state, exception);
            if (exception != null)
                message += Environment.NewLine + exception.ToString();

            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {_categoryName}: {message}";

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_filePath, logEntry + Environment.NewLine);
                }
                catch
                {
                    // Silently fail if we can't write to log file
                }
            }
        }
    }
}
