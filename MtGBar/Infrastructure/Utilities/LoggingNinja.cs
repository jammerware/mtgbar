﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MtGBar.Infrastructure.Utilities
{
    public class LoggingNinja
    {
        #region Fields
        private List<string> _MissedMessages;
        #endregion

        public string LogFileName { get; private set; }

        #region Constructor
        public LoggingNinja(string logFileName)
        {
            if (string.IsNullOrEmpty(logFileName)) {
                throw new InvalidOperationException("Specify a log file name.");
            }

            LogFileName = logFileName;
            _MissedMessages = new List<string>();
        }
        #endregion

        #region Methods
        public void LogError(Exception e)
        {
            LogMessage(e.GetType().ToString() + ": " + e.Message);
        }

        public void LogMessage(string message)
        {
            StringBuilder contents = new StringBuilder();
            string stampedMessage = DateTime.Now.ToShortDateString() + "@" + DateTime.Now.ToLongTimeString() + " - " + message;

            try {
                using (FileStream stream = File.Open(LogFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
                    // max 10 MB
                    if (stream.Length < 10485760) {
                        using (StreamReader reader = new StreamReader(stream)) {
                            contents.Append(reader.ReadToEnd());
                        }
                    }
                }

                foreach (string missedMessage in _MissedMessages) {
                    contents.AppendLine("MM: " + _MissedMessages);
                }
                _MissedMessages.Clear();

                contents.AppendLine(stampedMessage);
                File.WriteAllText(LogFileName, contents.ToString());
            }
            catch (IOException) {
                _MissedMessages.Add(stampedMessage);
                while (_MissedMessages.Count > 10) {
                    _MissedMessages.RemoveAt(10);
                }
            }
        }
        #endregion
    }
}