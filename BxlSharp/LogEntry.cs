﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BxlSharp
{
    /// <summary>
    /// List of information about the BXL data read, including errors encountered.
    /// </summary>
    public class Logs : IReadOnlyList<LogEntry>
    {
        internal Logs()
        {

        }

        internal Logs(Logs logs)
        {
            Data = new List<LogEntry>(logs.Data);
        }

        internal List<LogEntry> Data { get; } = new List<LogEntry>();
        public LogEntry this[int index] => Data[index];
        public int Count => Data.Count;
        public IEnumerator<LogEntry> GetEnumerator() => Data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Data.GetEnumerator();

        /// <summary>
        /// Warnings of possible problems during reading the BXL data.
        /// </summary>
        public IEnumerable<string> Warnings => Data.Where(l => l.Severity == LogSeverity.Warning).Select(l => l.Message);
        
        /// <summary>
        /// Error information reading the BXL data.
        /// </summary>
        public IEnumerable<string> Errors => Data.Where(l => l.IsError).Select(l => l.Message);
        
        /// <summary>
        /// If the logs has any error encountered during the parsing of the BXL data then the result data
        /// may be incomplete or inaccurate as errors interrupt the parsing process.
        /// </summary>
        public bool HasErrors => Errors.Any();
    }

    /// <summary>
    /// Entry in the list of messages generated by the BXL parser while reading the data.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Indicates the severity of the log entry.
        /// </summary>
        public LogSeverity Severity { get; }

        /// <summary>
        /// Message text of the log entry.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Is this log entry an error.
        /// </summary>
        public bool IsError => Severity == LogSeverity.Error || Severity == LogSeverity.InternalError;

        internal LogEntry(LogSeverity severity, string message) =>
            (Severity, Message) = (severity, message);
    }

    public enum LogSeverity
    {
        Information,
        Warning,
        Error,
        InternalError
    }
}