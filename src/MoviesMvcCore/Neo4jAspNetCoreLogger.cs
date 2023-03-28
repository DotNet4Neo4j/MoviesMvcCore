namespace MoviesMvcCore
{
    using System;
    using System.Collections;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using Neo4j.Driver;
    using ILogger = Neo4j.Driver.ILogger;

    /// <summary>
    /// An <see cref="ILogger"/> implementation that hooks into the Asp NET <see cref="Microsoft.Extensions.Logging.ILogger"/> interface.
    /// </summary>
    public class Neo4jAspNetCoreLogger : ILogger
    {
        /// <summary>
        /// The <see cref="Microsoft.Extensions.Logging.ILogger"/> instance, injected in <c>startup.cs</c>
        /// </summary>
        private readonly ILogger<IDriver> _aspLogger;

        public Neo4jAspNetCoreLogger(ILogger<IDriver> aspLogger)
        {
            _aspLogger = aspLogger;
        }

        /// <summary>
        /// Set the <see cref="LogLevel"/> for the logger.
        /// </summary>
        public LogLevel Level { get; set; }

        public void Debug(string message, params object[] args)
        {
            Log(LogLevel.Debug, message, args);
        }

        public void Error(Exception cause, string message, params object[] args)
        {
            Log(LogLevel.Error, message, cause, args);
        }

        public void Info(string message, params object[] args)
        {
            Log(LogLevel.Information, message, args);
        }

        public bool IsDebugEnabled()
        {
            return Level >= LogLevel.Debug;
        }

        public bool IsTraceEnabled()
        {
            return Level >= LogLevel.Trace;
        }

        public void Trace(string message, params object[] args)
        {
            Log(LogLevel.Trace, message, args);
        }

        public void Warn(Exception cause, string message, params object[] args)
        {
            Log(LogLevel.Error, message, cause, args);
        }

        private void Log(LogLevel logLevel, string message, params object[] restOfMessage)
        {
            if (message == null)
                return;

            _aspLogger.Log(logLevel, $"{logLevel}>>: {Format(restOfMessage)}");
        }

        private static string Format(params object[] message)
        {
            if (message == null || !message.Any()) return string.Empty;
            return string.Join(Environment.NewLine, message.Select(Format));
        }

        private static string Format(object obj)
        {
            return obj switch
            {
                null => string.Empty,
                IEnumerable enumerable => string.Join(",", enumerable.Cast<object>().Select(o => o == null ? string.Empty : o.ToString())),
                _ => obj.ToString()
            };
        }
    }
}