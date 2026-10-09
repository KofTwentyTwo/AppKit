using KofTwentyTwo.AppKit.Interaction;
using KofTwentyTwo.AppKit.Logging;
using KofTwentyTwo.AppKit.Updates;

namespace KofTwentyTwo.AppKit.Tests;

/// <summary>A per-test temporary directory, deleted on dispose.</summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "appkit-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // A handle a failing test left open; the OS temp cleaner gets it later.
        }
    }
}

/// <summary>A settable clock in a fixed UTC+0 zone, so local time equals UTC.</summary>
internal sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

/// <summary>An <see cref="IActivityLog"/> that records entries in memory.</summary>
internal sealed class RecordingLog : IActivityLog
{
    public List<string> Entries { get; } = [];

    public string LogDirectory => "";

    public string CurrentLogFilePath => "";

    public void Info(string message) => Entries.Add("INFO " + message);

    public void Warning(string message) => Entries.Add("WARN " + message);

    public void Error(string message, Exception? exception = null) => Entries.Add("ERROR " + message);
}

/// <summary>An <see cref="IUserPrompter"/> that records prompts and answers confirmations from a script.</summary>
internal sealed class RecordingPrompter(bool confirm = false) : IUserPrompter
{
    public List<(string Title, string Message)> Messages { get; } = [];

    public List<(string Title, string Message, string ConfirmText, string CancelText)> Confirmations { get; } = [];

    public Task ShowMessageAsync(string title, string message)
    {
        Messages.Add((title, message));
        return Task.CompletedTask;
    }

    public Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText)
    {
        Confirmations.Add((title, message, confirmText, cancelText));
        return Task.FromResult(confirm);
    }
}

/// <summary>A scriptable <see cref="IUpdateService"/>.</summary>
internal sealed class FakeUpdateService : IUpdateService
{
    public bool IsSupported { get; set; } = true;

    public string? CurrentVersion { get; set; } = "1.0.0";

    public UpdateCheckResult NextCheck { get; set; } = UpdateCheckResult.UpToDate;

    public string? ApplyError { get; set; }

    public int ApplyCalls { get; private set; }

    public Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default) => Task.FromResult(NextCheck);

    public Task<string?> DownloadAndApplyAsync(IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        ApplyCalls++;
        return Task.FromResult(ApplyError);
    }
}

/// <summary>A scriptable Velopack stand-in.</summary>
internal sealed class FakeBackend : IUpdateBackend
{
    public bool IsInstalled { get; set; } = true;

    public string? CurrentVersion { get; set; } = "1.0.0";

    public string? Available { get; set; }

    public Exception? CheckFailure { get; set; }

    public Exception? DownloadFailure { get; set; }

    public int Downloads { get; private set; }

    public int Applies { get; private set; }

    public Task<string?> CheckAsync()
        => CheckFailure is null ? Task.FromResult(Available) : Task.FromException<string?>(CheckFailure);

    public Task DownloadAsync(Action<int>? progress, CancellationToken cancellationToken)
    {
        if (DownloadFailure is not null)
        {
            return Task.FromException(DownloadFailure);
        }
        Downloads++;
        progress?.Invoke(100);
        return Task.CompletedTask;
    }

    public void ApplyAndRestart() => Applies++;
}
