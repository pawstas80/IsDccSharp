namespace IsDccSharp.Viewer.Services;

using IsDccSharp.Core;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

internal sealed class SmartLogger : IAppLogger, IDisposable
{
    private static readonly Encoding LogEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private readonly BlockingCollection<LogEntry> queue = new(new ConcurrentQueue<LogEntry>());
    private readonly Task writerTask;
    private readonly string baseFileName;
    private readonly int retentionDays;
    private int isDisposed;

    public SmartLogger(string appName, int retentionDays = 14)
    {
        if (string.IsNullOrWhiteSpace(appName))
            throw new ArgumentException("Application name is required.", nameof(appName));

        baseFileName = SanitizeFileName(appName);
        this.retentionDays = retentionDays;
        LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ProjectInfo.AppName,
            "Logs");

        Directory.CreateDirectory(LogDirectory);
        var removedFiles = CleanupOldLogs();

        writerTask = Task.Factory.StartNew(
            WriterLoop,
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        Info($"{ProjectInfo.AppName} {ProjectInfo.Version} logger started.");
        if (removedFiles > 0)
            Info($"Removed {removedFiles} old log file(s).");
    }

    public string CurrentLogFile => Path.Combine(LogDirectory, $"{baseFileName}_{DateTime.Now:yyyy-MM-dd}.log");

    public string LogDirectory { get; }

    public void Info(string message) => Write("INFO", message);

    public void Warn(string message) => Write("WARN", message);

    public void Error(string message) => Write("ERROR", message);

    public void Exception(string context, Exception exception)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        var current = exception;
        var prefix = string.IsNullOrWhiteSpace(context) ? "Exception" : context;

        while (current != null)
        {
            Write("EXCEPTION", $"{prefix}: {current.GetType().FullName}: {current.Message}");

            if (!string.IsNullOrWhiteSpace(current.Source))
                Write("SOURCE", current.Source);

            if (!string.IsNullOrWhiteSpace(current.StackTrace))
                Write("STACK", current.StackTrace);

            current = current.InnerException;
            prefix = "Caused by";
        }
    }

    public IDisposable Time(string operationName)
    {
        return new TimerScope(this, operationName);
    }

    public void HookExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskSchedulerUnobservedTaskException;
    }

    public void UnhookExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException -= CurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= TaskSchedulerUnobservedTaskException;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref isDisposed, 1) != 0)
            return;

        try
        {
            Info("Logger shutting down.");
            queue.CompleteAdding();
            writerTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Logging shutdown must never block application exit.
        }
        finally
        {
            queue.Dispose();
        }
    }

    private void Write(string level, string message)
    {
        if (Volatile.Read(ref isDisposed) != 0 || queue.IsAddingCompleted)
            return;

        try
        {
            queue.Add(new LogEntry(DateTime.Now, level, message ?? string.Empty));
        }
        catch (InvalidOperationException)
        {
            // The queue is closing; dropping late log entries is safer than crashing shutdown.
        }
    }

    private void WriterLoop()
    {
        foreach (var entry in queue.GetConsumingEnumerable())
        {
            var line = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{entry.Level}] {entry.Message}";

            try
            {
                File.AppendAllText(CurrentLogFile, line + Environment.NewLine, LogEncoding);
                Debug.WriteLine(line);
            }
            catch
            {
                // File-system logging errors are intentionally ignored.
            }
        }
    }

    private int CleanupOldLogs()
    {
        try
        {
            var removed = 0;
            foreach (var file in Directory.GetFiles(LogDirectory, $"{baseFileName}_*.log"))
            {
                var info = new FileInfo(file);
                if (info.LastWriteTime >= DateTime.Now.AddDays(-retentionDays))
                    continue;

                try
                {
                    info.Delete();
                    removed++;
                }
                catch
                {
                    // Ignore individual cleanup failures.
                }
            }

            return removed;
        }
        catch
        {
            return 0;
        }
    }

    private void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
            Exception("Unhandled AppDomain exception", exception);
        else
            Error("Unhandled AppDomain exception: " + e.ExceptionObject);

        if (e.IsTerminating)
            Error("CLR is terminating after an unhandled exception.");
    }

    private void TaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Exception("Unobserved task exception", e.Exception);
        e.SetObserved();
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
            builder.Append(Array.IndexOf(invalid, character) >= 0 ? '_' : character);

        return builder.ToString();
    }

    private readonly struct LogEntry
    {
        public LogEntry(DateTime timestamp, string level, string message)
        {
            Timestamp = timestamp;
            Level = level;
            Message = message;
        }

        public DateTime Timestamp { get; }
        public string Level { get; }
        public string Message { get; }
    }

    private sealed class TimerScope : IDisposable
    {
        private readonly SmartLogger logger;
        private readonly string operationName;
        private readonly Stopwatch stopwatch;
        private int isDisposed;

        public TimerScope(SmartLogger logger, string operationName)
        {
            this.logger = logger;
            this.operationName = string.IsNullOrWhiteSpace(operationName) ? "Operation" : operationName;
            stopwatch = Stopwatch.StartNew();
            logger.Info($"{this.operationName} started.");
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref isDisposed, 1) != 0)
                return;

            stopwatch.Stop();
            logger.Info($"{operationName} completed in {stopwatch.ElapsedMilliseconds.ToString("N0", CultureInfo.InvariantCulture)} ms.");
        }
    }
}
