/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Interaction;
using KofTwentyTwo.AppKit.Logging;
using KofTwentyTwo.AppKit.Updates;


namespace KofTwentyTwo.AppKit.Tests;

/// <summary>A per-test temporary directory, deleted on dispose.</summary>
internal sealed class TempDirectory : IDisposable
{
   /// <summary>Creates a unique folder under the system temp directory.</summary>
   public TempDirectory()
   {
      Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "appkit-tests", Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(Path);
   }



   public string Path { get; }



   /// <summary>A path inside the folder.</summary>
   public string File(string name) => System.IO.Path.Combine(Path, name);



   /// <summary>Deletes the folder; a handle a failing test left open is left for the OS temp cleaner.</summary>
   public void Dispose()
   {
      try
      {
         Directory.Delete(Path, recursive: true);
      }
      catch(IOException)
      {
         // A handle a failing test left open; the OS temp cleaner gets it later.
      }
   }
}



/// <summary>A settable clock in a fixed UTC+0 zone, so local time equals UTC.</summary>
internal sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
{
   public DateTimeOffset Now { get; set; } = now;



   /// <summary>The settable current time.</summary>
   public override DateTimeOffset GetUtcNow() => Now;



   public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}



/// <summary>An <see cref="IActivityLog"/> that records entries in memory.</summary>
internal sealed class RecordingLog : IActivityLog
{
   public List<string> Entries { get; } = [];

   public string LogDirectory => "";

   public string CurrentLogFilePath => "";



   /// <summary>Records an INFO entry.</summary>
   public void Info(string message) => Entries.Add("INFO " + message);



   /// <summary>Records a WARN entry.</summary>
   public void Warning(string message) => Entries.Add("WARN " + message);



   /// <summary>Records an ERROR entry (the exception is not needed by the tests).</summary>
   public void Error(string message, Exception? exception = null) => Entries.Add("ERROR " + message);
}



/// <summary>An <see cref="IUserPrompter"/> that records prompts and answers confirmations from a script.</summary>
internal sealed class RecordingPrompter(bool confirm = false) : IUserPrompter
{
   public List<(string Title, string Message)> Messages { get; } = [];

   public List<(string Title, string Message, string ConfirmText, string CancelText)> Confirmations { get; } = [];



   /// <summary>Records the message.</summary>
   public Task ShowMessageAsync(string title, string message)
   {
      Messages.Add((title, message));
      return Task.CompletedTask;
   }



   /// <summary>Records the question and answers with the scripted choice.</summary>
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

   public int CheckCalls { get; private set; }



   /// <summary>Returns the scripted check result.</summary>
   public Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
   {
      CheckCalls++;
      return Task.FromResult(NextCheck);
   }



   /// <summary>Counts the call and returns the scripted error, or null for success.</summary>
   public Task<string?> DownloadAndApplyAsync(IProgress<int>? progress = null, CancellationToken cancellationToken = default)
   {
      ApplyCalls++;
      return Task.FromResult(ApplyError);
   }
}



/// <summary>A scriptable Velopack stand-in.</summary>
internal sealed class FakeBackend : IUpdateBackend
{
   private string? _currentVersion = "1.0.0";

   public bool IsInstalled { get; set; } = true;

   public string? CurrentVersion
   {
      get => VersionFailure is null ? _currentVersion : throw VersionFailure;
      set => _currentVersion = value;
   }

   public Exception? VersionFailure { get; set; }

   public string? Available { get; set; }

   public Exception? CheckFailure { get; set; }

   public Exception? DownloadFailure { get; set; }

   public int Downloads { get; private set; }

   public int Applies { get; private set; }



   /// <summary>Returns the scripted available version, or fails with the scripted exception.</summary>
   public Task<string?> CheckAsync()
       => CheckFailure is null ? Task.FromResult(Available) : Task.FromException<string?>(CheckFailure);



   /// <summary>Counts the download and reports 100% progress, or fails with the scripted exception.</summary>
   public Task DownloadAsync(Action<int>? progress, CancellationToken cancellationToken)
   {
      if(DownloadFailure is not null)
      {
         return Task.FromException(DownloadFailure);
      }
      Downloads++;
      progress?.Invoke(100);
      return Task.CompletedTask;
   }



   /// <summary>Counts the apply; the real backend would exit the process here.</summary>
   public void ApplyAndRestart() => Applies++;
}
