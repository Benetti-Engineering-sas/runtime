// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// A logger that writes messages in the console.
    /// </summary>
    [UnsupportedOSPlatform("browser")]
    internal sealed class ConsoleLogger : ILogger, IBufferedLogger
    {
        private readonly string _name;
        private readonly ConsoleLoggerProcessor _queueProcessor;

        internal ConsoleLogger(
            string name,
            ConsoleLoggerProcessor loggerProcessor,
            ConsoleFormatter formatter,
            IExternalScopeProvider? scopeProvider,
            ConsoleLoggerOptions options)
        {
            ArgumentNullException.ThrowIfNull(name);

            _name = name;
            _queueProcessor = loggerProcessor;
            Formatter = formatter;
            ScopeProvider = scopeProvider;
            Options = options;
        }

        internal ConsoleFormatter Formatter { get; set; }
        internal IExternalScopeProvider? ScopeProvider { get; set; }
        internal ConsoleLoggerOptions Options { get; set; }

        [ThreadStatic]
        private static StringWriter? t_stringWriter;

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(formatter);

            t_stringWriter ??= new StringWriter();
            LogEntry<TState> logEntry = new LogEntry<TState>(logLevel, _name, eventId, state, exception, formatter);
            Formatter.Write(in logEntry, ScopeProvider, t_stringWriter);

            var sb = t_stringWriter.GetStringBuilder();
            if (sb.Length == 0)
            {
                return;
            }
            string computedAnsiString = sb.ToString();
            sb.Clear();
            if (sb.Capacity > 1024)
            {
                sb.Capacity = 1024;
            }
            _queueProcessor.EnqueueMessage(new LogMessageEntry(computedAnsiString, logAsError: logLevel >= Options.LogToStandardErrorThreshold));
        }

        /// <inheritdoc />
        public void LogRecords(IEnumerable<BufferedLogRecord> records)
        {
            ArgumentNullException.ThrowIfNull(records);

            StringWriter writer = t_stringWriter ??= new StringWriter();

            var sb = writer.GetStringBuilder();
            foreach (var rec in records)
            {
                var logEntry = new LogEntry<BufferedLogRecord>(rec.LogLevel, _name, rec.EventId, rec, null, static (s, _) => s.FormattedMessage ?? string.Empty);
                Formatter.Write(in logEntry, null, writer);

                if (sb.Length == 0)
                {
                    continue;
                }

                string computedAnsiString = sb.ToString();
                sb.Clear();
                _queueProcessor.EnqueueMessage(new LogMessageEntry(computedAnsiString, logAsError: rec.LogLevel >= Options.LogToStandardErrorThreshold));
            }

            if (sb.Capacity > 1024)
            {
                sb.Capacity = 1024;
            }
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => ScopeProvider?.Push(state) ?? NullScope.Instance;
    }
}
