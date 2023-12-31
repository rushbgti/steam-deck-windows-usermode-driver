﻿using Microsoft.Extensions.Logging;
using SWICD.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SWICD.Services
{
    internal class LoggingService
    {
        private static LoggingService _instance = new LoggingService();
        public static LoggingService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new LoggingService();
                return _instance;
            }
        }
        public List<LogEntryModel> LogEntries = new List<LogEntryModel>();
        private string file = $"driver_log_{DateTime.Now.Year}_{DateTime.Now.Month:0#}_{DateTime.Now.Day:0#}.log";
        private FileStream LogStream = null;
        private StreamWriter LogWriter = null;

        public LoggingService()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            folder = Path.Combine(folder, "SWICD");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            file = Path.Combine(folder, file);

            LogStream = File.OpenWrite(file);
            LogStream.Position = LogStream.Length;

            LogWriter = new StreamWriter(LogStream);

            string[] files = Directory.GetFiles(folder, "*.log");
            foreach(var filename in files)
            {
                FileInfo info = new FileInfo(filename);
                if(info.LastWriteTime < DateTime.Now.AddDays(-30) && filename != file)
                {
                    File.Delete(filename);
                    LogDebug($"Deleting Log File {Path.GetFileName(filename)} as its older than 30 days.");
                }
            }
        }

        public event EventHandler<LogEntryModel> OnNewLogEntry;

        public void Log(LogLevel level, string message)
        {
            var entry = new LogEntryModel()
            {
                LogLevel = level,
                Message = message,
                Time = DateTime.Now,
            };
            LogEntries.Add(entry);
            _ = Task.Run(() => OnNewLogEntry?.Invoke(this, entry));
            lock (LogWriter)
                LogWriter.WriteLine($"[{entry.Time}][{level}]: {message}\r\n");
        }

        public string GetLogString()
        {
            return String.Join("\r\n", LogEntries.Select(entry => $"[{entry.Time}][{entry.LogLevel}]: {entry.Message}"));
        }

        public static void LogInformation(string message) => Instance?.Log(LogLevel.Information, message);
        public static void LogWarning(string message) => Instance?.Log(LogLevel.Warning, message);
        public static void LogError(string message) => Instance?.Log(LogLevel.Error, message);
        public static void LogDebug(string message) => Instance?.Log(LogLevel.Debug, message);
        public static void LogCritical(string message) => Instance?.Log(LogLevel.Critical, message);
    }
}
