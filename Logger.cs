/*
 * Copyright © 2017-2020 Starkku
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Starkku.Utilities
{
    /// <summary>
    /// A logger class that either writes to a console or calls a provided method, and optionally writes to a text file.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Gets or sets whether or not logger has been properly initialized.
        /// </summary>
        public static bool Initialized { get; set; }

        /// <summary>
        /// Gets or sets whether or not logger writes to console.
        /// </summary>
        public static bool WriteToConsole { get; set; } = true;

        private static bool _writeFile = false;

        /// <summary>
        /// Gets or sets whether or not logger writes to a file.
        /// </summary>
        public static bool WriteFile
        {
            get => _writeFile;
            set
            {
                if (!_writeFile && value && ((logWriter != null && (logWriter.BaseStream as FileStream).Name != Filename) || logWriter == null))
                    InitializeLogWriter();

                _writeFile = value;
            }
        }

        /// <summary>
        /// Gets or sets whether or not logger writes timestamps when logging to console or provided methods.
        /// </summary>
        public static bool WriteTimestamps { get; set; } = true;

        /// <summary>
        /// Gets or sets whether or not logger writes log level labels when logging to console or provided methods.
        /// </summary>
        public static bool WriteLogLevelLabels { get; set; } = true;

        /// <summary>
        /// Gets or sets whether or not debug-level logging is enabled outside writing to a log file.
        /// </summary>
        public static bool EnableDebugLogging { get; set; }

        private static string _filename = null;

        /// <summary>
        /// List of methods called with log message as parameter every time something is logged.
        /// </summary>
        public static List<Action<string>> LogMessageActions { get; private set; } = new List<Action<string>>();

        /// <summary>
        /// Gets or sets filename of log file being written to if writing to file is enabled.
        /// </summary>
        public static string Filename
        {
            get => _filename;
            set
            {
                if (value != _filename)
                {
                    _filename = value;

                    if (WriteFile)
                        InitializeLogWriter();
                }
            }
        }

        private static Stopwatch timestampTimer = null;
        private static ConsoleColor defaultConsoleColor;
        private static StreamWriter logWriter = null;
        private static readonly object locker = new object();

        /// <summary>
        /// Initializes a logger that writes to console and optionally to a log with default filename.
        /// </summary>
        /// <param name="writeToFile">If set, writes the log to a file.</param>
        /// <param name="enableDebugLogging">If set, debug-level logging is enabled outside written log files.</param>
        public static void Initialize(bool writeToFile = false, bool enableDebugLogging = false)
        {
            string filename = AppDomain.CurrentDomain.BaseDirectory + Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) + ".log";
            Initialize(filename, writeToFile, enableDebugLogging);
        }

        /// <summary>
        /// Initializes a logger that calls the provided method with log message as parameter and optionally writes to a log with default filename.
        /// </summary>
        /// <param name="logMessageAction">A method to call with the log message.</param>
        /// <param name="writeToFile">If set, writes the log to a file.</param>
        /// <param name="enableDebugLogging">If set, debug-level logging is enabled outside written log files.</param>
        public static void Initialize(Action<string> logMessageAction, bool writeToFile = false, bool enableDebugLogging = false)
        {
            string filename = AppDomain.CurrentDomain.BaseDirectory + Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) + ".log";
            Initialize(logMessageAction, filename, writeToFile, enableDebugLogging);
        }

        /// <summary>
        /// Initializes a logger that writes to console and optionally to a log with specified filename.
        /// </summary>
        /// <param name="filename">Filename of the log file to write.</param>
        /// <param name="writeToFile">If set, writes the log to a file.</param>
        /// <param name="enableDebugLogging">If set, debug-level logging is enabled outside written log files.</param>
        public static void Initialize(string filename, bool writeToFile = true, bool enableDebugLogging = false)
        {
            Filename = filename;
            WriteFile = writeToFile;
#if DEBUG
            EnableDebugLogging = true;
#else
            EnableDebugLogging = enableDebugLogging;
#endif
            InitializeProperties();
            Initialized = true;
        }

        /// <summary>
        /// Initializes a logger that calls the provided method with log message as parameter and optionally writes to a log with specified filename.
        /// </summary>
        /// <param name="logMessageAction">A method to call with the log message.</param>
        /// <param name="filename">Filename of the log file to write.</param>
        /// <param name="writeToFile">If set, writes the log to a file.</param>
        /// <param name="enableDebugLogging">If set, debug-level logging is enabled outside written log files.</param>
        public static void Initialize(Action<string> logMessageAction, string filename, bool writeToFile = true, bool enableDebugLogging = false)
        {
            Initialize(filename, writeToFile, enableDebugLogging);
            LogMessageActions.Add(logMessageAction);
            WriteToConsole = false;
        }

        private static void InitializeProperties()
        {
            timestampTimer = new Stopwatch();
            timestampTimer.Start();
            defaultConsoleColor = Console.ForegroundColor;
        }

        private static void InitializeLogWriter()
        {
            if (File.Exists(Filename))
                File.Delete(Filename);

            logWriter = new StreamWriter(Filename)
            {
                AutoFlush = true
            };
        }

        /// <summary>
        /// Log a string as general info.
        /// </summary>
        /// <param name="logMessage">String to log.</param>
        public static void Info(string logMessage)
        {
            Log("[Info]", logMessage, ConsoleColor.Gray);
        }

        /// <summary>
        /// Log a string as general info with custom color.
        /// </summary>
        /// <param name="logMessage">String to log.</param>
        /// <param name="color">Custom message color.</param>
        public static void Info(string logMessage, ConsoleColor color)
        {
            Log("[Info]", logMessage, color);
        }

        /// <summary>
        /// Logs a string as a warning.
        /// </summary>
        /// <param name="logMessage">String to log.</param>
        public static void Warn(string logMessage)
        {
            Log("[Warn]", logMessage, ConsoleColor.Yellow);
        }

        /// <summary>
        /// Logs a string as an error.
        /// </summary>
        /// <param name="logMessage">String to log.</param>
        public static void Error(string logMessage)
        {
            Log("[Error]", logMessage, ConsoleColor.Red);
        }

        /// <summary>
        /// Logs a string as a debug message. 
        /// Only written to console / provided method if debug logging is enabled.
        /// </summary>
        /// <param name="logMessage">String to log.</param>
        public static void Debug(string logMessage)
        {
            Log("[Debug]", logMessage, ConsoleColor.DarkGray, !EnableDebugLogging);
        }

        /// <summary>
        /// Logs a string.
        /// </summary>
        /// <param name="logLevelLabel">Level label of the log message.</param>
        /// <param name="logMessage">String to log.</param>
        /// <param name="consoleColor">Color used to display log message in console.</param>
        /// <param name="onlyLogToFile">If set, only logs to file.</param>
        private static void Log(string logLevelLabel, string logMessage, ConsoleColor consoleColor, bool onlyLogToFile = false)
        {
            if (!Initialized)
                return;

            lock (locker)
            {
                if (!onlyLogToFile)
                {
                    string logMessageAppended = (WriteTimestamps ? GetSeconds() + " " : "") + (WriteLogLevelLabels ? logLevelLabel + " " : "") + logMessage;

                    if (WriteToConsole)
                    {
                        Console.ForegroundColor = consoleColor;
                        Console.WriteLine(logMessageAppended);
                        Console.ForegroundColor = defaultConsoleColor;
                    }

                    foreach (Action<string> action in LogMessageActions)
                    {
                        action(logMessageAppended);
                    }
                }

                LogToFile(logLevelLabel, logMessage);
            }
        }

        /// <summary>
        /// Logs a string only to a file, using a specific label to precede the logged string.
        /// </summary>
        /// <param name="logLevelLabel">Level label of the log message.</param>
        /// <param name="logMessage">String to log.</param>
        private static void LogToFile(string logLevelLabel, string logMessage)
        {
            if (!Initialized || !WriteFile || logWriter == null || !logWriter.BaseStream.CanWrite)
                return;

            logWriter.WriteLine(GetDateTime() + " " + logLevelLabel + " " + logMessage);
        }

        /// <summary>
        /// Logs a string only to a file.
        /// </summary>
        /// <param name="logMessage">String to log.</param>
        public static void LogToFileOnly(string logMessage)
        {
            lock (locker)
            {
                if (!Initialized || !WriteFile || logWriter == null || !logWriter.BaseStream.CanWrite)
                    return;

                logWriter.WriteLine(logMessage);
            }
        }

        /// <summary>
        /// Gets current timer time in seconds.
        /// </summary>
        /// <returns>Current timer time in seconds.</returns>
        private static string GetSeconds()
        {
            return timestampTimer.Elapsed.TotalSeconds.ToString("0.00000", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets current timer time as a date string.
        /// </summary>
        /// <returns>Current timer time as a date string.</returns>
        private static string GetDateTime()
        {
            string dateString = DateTime.Now.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            return dateString + " | " + timestampTimer.Elapsed.ToString();
        }
    }
}
