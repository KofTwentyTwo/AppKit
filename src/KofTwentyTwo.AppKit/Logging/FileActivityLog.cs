using System.Globalization;

namespace KofTwentyTwo.AppKit.Logging;

/// <summary>
/// An <see cref="IActivityLog"/> that appends timestamped lines to a daily file,
/// &lt;prefix&gt;-yyyy-MM-dd.log. Writes are serialized by a lock and each one opens,
/// appends, and closes the file, so every entry reaches disk even if the process dies
/// right after. Logging must never take the application down: all IO failures
/// (unwritable directory, locked file, full disk) are swallowed. The directory is
/// created lazily on the first write, and files older than
/// <see cref="RetentionDays"/> are pruned once per day on the first write of that day.
/// </summary>
public sealed class FileActivityLog : IActivityLog
{
    /// <summary>Default number of days of log files kept.</summary>
    public const int DefaultRetentionDays = 30;

    private const string DateFormat = "yyyy-MM-dd";

    private readonly object _gate = new();
    private readonly string _filePrefix;
    private readonly TimeProvider _time;
    private DateOnly _lastPruned;

    /// <summary>Creates a log that writes under <paramref name="directory"/>.</summary>
    /// <param name="directory">Folder for the log files.</param>
    /// <param name="filePrefix">File name prefix, normally the app id.</param>
    /// <param name="retentionDays">Days of files to keep; 0 or less keeps everything.</param>
    /// <param name="time">Clock; the system clock when null.</param>
    public FileActivityLog(string directory, string filePrefix, int retentionDays = DefaultRetentionDays, TimeProvider? time = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePrefix);
        LogDirectory = directory;
        _filePrefix = filePrefix;
        RetentionDays = retentionDays;
        _time = time ?? TimeProvider.System;
    }

    /// <summary>Creates the standard log for an app: &lt;data&gt;\logs\&lt;id&gt;-yyyy-MM-dd.log.</summary>
    public static FileActivityLog ForApp(AppInfo app, AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(paths);
        return new FileActivityLog(paths.LogsDirectory, app.Id);
    }

    /// <inheritdoc/>
    public string LogDirectory { get; }

    /// <summary>Days of log files kept; 0 or less disables pruning.</summary>
    public int RetentionDays { get; }

    /// <summary>Today's log file; entries roll over to a new file at local midnight.</summary>
    public string CurrentLogFilePath => FilePathFor(Today);

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    /// <inheritdoc/>
    public void Info(string message) => Write("INFO", message, exception: null);

    /// <inheritdoc/>
    public void Warning(string message) => Write("WARN", message, exception: null);

    /// <inheritdoc/>
    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private string FilePathFor(DateOnly day)
        => Path.Combine(LogDirectory, $"{_filePrefix}-{day.ToString(DateFormat, CultureInfo.InvariantCulture)}.log");

    private void Write(string level, string message, Exception? exception)
    {
        try
        {
            lock (_gate)
            {
                Directory.CreateDirectory(LogDirectory);
                PruneOncePerDay();

                string timestamp = _time.GetLocalNow().ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                string entry = $"{timestamp} [{level}] {message}";
                if (exception is not null)
                {
                    entry += Environment.NewLine + exception;
                }

                File.AppendAllText(CurrentLogFilePath, entry + Environment.NewLine);
            }
        }
        catch
        {
            // A logging failure must never take the operation it describes down.
        }
    }

    /// <summary>Deletes this log's files dated before the retention window. Caller holds the lock.</summary>
    private void PruneOncePerDay()
    {
        DateOnly today = Today;
        if (RetentionDays <= 0 || _lastPruned == today)
        {
            return;
        }
        _lastPruned = today;

        DateOnly oldestKept = today.AddDays(-(RetentionDays - 1));
        foreach (string file in Directory.EnumerateFiles(LogDirectory, _filePrefix + "-*.log"))
        {
            string stamp = Path.GetFileNameWithoutExtension(file)[(_filePrefix.Length + 1)..];
            if (DateOnly.TryParseExact(stamp, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day)
                && day < oldestKept)
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // A file another process holds open is retried tomorrow.
                }
            }
        }
    }
}
