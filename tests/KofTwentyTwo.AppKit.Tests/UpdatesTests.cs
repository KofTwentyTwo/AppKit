/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Updates;


namespace KofTwentyTwo.AppKit.Tests;

/// <summary>Tests for update check results.</summary>
public class UpdateCheckResultTests
{
   /// <summary>Only a result with a version and no error counts as an available update.</summary>
   [Fact]
   public void Factories()
   {
      Assert.False(UpdateCheckResult.UpToDate.IsUpdateAvailable);
      Assert.True(UpdateCheckResult.Available("2.0.0").IsUpdateAvailable);
      Assert.False(UpdateCheckResult.Failed("x").IsUpdateAvailable);
      Assert.False(new UpdateCheckResult("2.0.0", "but failed").IsUpdateAvailable);
   }
}



/// <summary>Tests for the update service&apos;s guarding and state, over a fake Velopack backend.</summary>
public class VelopackUpdateServiceTests
{
   /// <summary>NotInstalled: is unsupported.</summary>
   [Fact]
   public async Task NotInstalled_IsUnsupported()
   {
      var service = new VelopackUpdateService(() => new FakeBackend { IsInstalled = false, CurrentVersion = null });
      Assert.False(service.IsSupported);
      Assert.Null(service.CurrentVersion);
      UpdateCheckResult result = await service.CheckAsync();
      Assert.Equal(UpdateText.NotSupported, result.Error);
   }



   /// <summary>BackendThatCannotBeCreated: is unsupported and never throws.</summary>
   [Fact]
   public async Task BackendThatCannotBeCreated_IsUnsupportedAndNeverThrows()
   {
      var service = new VelopackUpdateService(() => throw new InvalidOperationException("no manager"));
      Assert.False(service.IsSupported);
      Assert.Null(service.CurrentVersion);
      Assert.Equal("no manager", (await service.CheckAsync()).Error);
      Assert.NotNull(await service.DownloadAndApplyAsync());
   }



   /// <summary>Check: finds update then downloads applies with progress.</summary>
   [Fact]
   public async Task Check_FindsUpdate_ThenDownloadsAppliesWithProgress()
   {
      var backend = new FakeBackend { Available = "2.0.0" };
      var service = new VelopackUpdateService(() => backend);
      Assert.True(service.IsSupported);
      Assert.Equal("1.0.0", service.CurrentVersion);

      UpdateCheckResult result = await service.CheckAsync();
      Assert.Equal("2.0.0", result.AvailableVersion);

      var reported = new List<int>();
      Assert.Null(await service.DownloadAndApplyAsync(new SynchronousProgress(reported.Add)));
      Assert.Equal([100], reported);
      Assert.Null(await service.DownloadAndApplyAsync());
      Assert.Equal(2, backend.Downloads);
      Assert.Equal(2, backend.Applies);
   }



   /// <summary>Check: up to date leaves nothing to install.</summary>
   [Fact]
   public async Task Check_UpToDate_LeavesNothingToInstall()
   {
      var service = new VelopackUpdateService(() => new FakeBackend());
      Assert.Same(UpdateCheckResult.UpToDate, await service.CheckAsync());
      Assert.Equal("No update is ready to install; check for updates first.", await service.DownloadAndApplyAsync());
   }



   /// <summary>Check: failure is reported and clears pending update.</summary>
   [Fact]
   public async Task Check_Failure_IsReportedAndClearsPendingUpdate()
   {
      var backend = new FakeBackend { Available = "2.0.0" };
      var service = new VelopackUpdateService(() => backend);
      await service.CheckAsync();
      backend.CheckFailure = new HttpRequestException("offline");
      Assert.Equal("offline", (await service.CheckAsync()).Error);
      Assert.NotNull(await service.DownloadAndApplyAsync());
      Assert.Equal(0, backend.Downloads);
   }



   /// <summary>DownloadFailure: is returned.</summary>
   [Fact]
   public async Task DownloadFailure_IsReturned()
   {
      var backend = new FakeBackend { Available = "2.0.0", DownloadFailure = new IOException("disk full") };
      var service = new VelopackUpdateService(() => backend);
      await service.CheckAsync();
      Assert.Equal("disk full", await service.DownloadAndApplyAsync());
      Assert.Equal(0, backend.Applies);
   }



   /// <summary>RealVelopack: outside an install is unsupported.</summary>
   [Fact]
   public async Task RealVelopack_OutsideAnInstall_IsUnsupported()
   {
      var service = new VelopackUpdateService(new Uri("https://github.com/KofTwentyTwo/AppKit"));
      Assert.False(service.IsSupported);
      Assert.Null(service.CurrentVersion);
      Assert.NotNull((await service.CheckAsync()).Error);
      Assert.False(new VelopackUpdateService(new Uri("https://github.com/KofTwentyTwo/AppKit"), includePrereleases: true).IsSupported);
   }



   /// <summary>NullRepository: throws.</summary>
   [Fact]
   public void NullRepository_Throws()
   {
      Assert.Throws<ArgumentNullException>(() => new VelopackUpdateService(null!, includePrereleases: false));
   }



   /// <summary>An IProgress that reports immediately on the calling thread, so assertions see the values.</summary>
   private sealed class SynchronousProgress(Action<int> report) : IProgress<int>
   {
      /// <summary>Forwards the value to the callback.</summary>
      public void Report(int value) => report(value);
   }
}



/// <summary>Tests for the interactive and quiet update flows.</summary>
public class UpdateCoordinatorTests
{
   private readonly FakeUpdateService _updates = new();
   private readonly RecordingLog _log = new();



   /// <summary>A coordinator over the fake service and the given prompter, logging to the recording log.</summary>
   private UpdateCoordinator Coordinator(RecordingPrompter prompter) => new(_updates, prompter, "Sample", _log);



   /// <summary>Interactive: unsupported says so.</summary>
   [Fact]
   public async Task Interactive_Unsupported_SaysSo()
   {
      _updates.IsSupported = false;
      var prompter = new RecordingPrompter();
      await Coordinator(prompter).CheckInteractivelyAsync();
      Assert.Equal((UpdateText.CheckTitle, UpdateText.NotSupported), prompter.Messages.Single());
   }



   /// <summary>Interactive: failure explains and logs.</summary>
   [Fact]
   public async Task Interactive_Failure_ExplainsAndLogs()
   {
      _updates.NextCheck = UpdateCheckResult.Failed("offline");
      var prompter = new RecordingPrompter();
      await Coordinator(prompter).CheckInteractivelyAsync();
      Assert.Equal("Could not check for updates.\noffline", prompter.Messages.Single().Message);
      Assert.Equal("WARN Update check failed: offline", _log.Entries.Single());
   }



   /// <summary>Interactive: up to date shows current version.</summary>
   [Theory]
   [InlineData("1.0.0", "You are up to date (v1.0.0).")]
   [InlineData(null, "You are up to date.")]
   public async Task Interactive_UpToDate_ShowsCurrentVersion(string? current, string expected)
   {
      _updates.CurrentVersion = current;
      var prompter = new RecordingPrompter();
      await Coordinator(prompter).CheckInteractivelyAsync();
      Assert.Equal(expected, prompter.Messages.Single().Message);
   }



   /// <summary>Interactive: available declined does not install.</summary>
   [Fact]
   public async Task Interactive_Available_Declined_DoesNotInstall()
   {
      _updates.NextCheck = UpdateCheckResult.Available("2.0.0");
      var prompter = new RecordingPrompter(confirm: false);
      await Coordinator(prompter).CheckInteractivelyAsync();
      (string Title, string Message, string ConfirmText, string CancelText) confirmation = prompter.Confirmations.Single();
      Assert.Equal(UpdateText.AvailableTitle, confirmation.Title);
      Assert.Equal("Sample v2.0.0 is available. The app will restart to finish installing the update.", confirmation.Message);
      Assert.Equal(UpdateText.Confirm, confirmation.ConfirmText);
      Assert.Equal(UpdateText.Decline, confirmation.CancelText);
      Assert.Equal(0, _updates.ApplyCalls);
   }



   /// <summary>Interactive: available accepted installs.</summary>
   [Fact]
   public async Task Interactive_Available_Accepted_Installs()
   {
      _updates.NextCheck = UpdateCheckResult.Available("2.0.0");
      var prompter = new RecordingPrompter(confirm: true);
      await Coordinator(prompter).CheckInteractivelyAsync();
      Assert.Equal(1, _updates.ApplyCalls);
      Assert.Empty(prompter.Messages);
      Assert.Equal("INFO Installing update v2.0.0.", _log.Entries.Single());
   }



   /// <summary>Install: failure is shown and logged.</summary>
   [Fact]
   public async Task Install_Failure_IsShownAndLogged()
   {
      _updates.NextCheck = UpdateCheckResult.Available("2.0.0");
      _updates.ApplyError = "disk full";
      var prompter = new RecordingPrompter(confirm: true);
      await Coordinator(prompter).CheckInteractivelyAsync();
      Assert.Equal((UpdateText.FailedTitle, "disk full"), prompter.Messages.Single());
      Assert.Equal("ERROR Update to v2.0.0 failed: disk full", _log.Entries.Last());
   }



   /// <summary>Quiet: unsupported up to date and failure stay silent.</summary>
   [Fact]
   public async Task Quiet_Unsupported_UpToDate_AndFailure_StaySilent()
   {
      var prompter = new RecordingPrompter();
      _updates.IsSupported = false;
      await Coordinator(prompter).CheckQuietlyAsync();

      _updates.IsSupported = true;
      await Coordinator(prompter).CheckQuietlyAsync();

      _updates.NextCheck = UpdateCheckResult.Failed("offline");
      await Coordinator(prompter).CheckQuietlyAsync();

      Assert.Empty(prompter.Messages);
      Assert.Empty(prompter.Confirmations);
      Assert.Equal("WARN Startup update check failed: offline", _log.Entries.Single());
   }



   /// <summary>Quiet: available offers the update.</summary>
   [Fact]
   public async Task Quiet_Available_OffersTheUpdate()
   {
      _updates.NextCheck = UpdateCheckResult.Available("2.0.0");
      var prompter = new RecordingPrompter(confirm: true);
      await Coordinator(prompter).CheckQuietlyAsync();
      Assert.Single(prompter.Confirmations);
      Assert.Equal(1, _updates.ApplyCalls);
      Assert.Equal("INFO Update available: v2.0.0.", _log.Entries[0]);
   }



   /// <summary>NullLog: is allowed.</summary>
   [Fact]
   public async Task NullLog_IsAllowed()
   {
      _updates.NextCheck = UpdateCheckResult.Failed("offline");
      await new UpdateCoordinator(_updates, new RecordingPrompter(), "Sample").CheckQuietlyAsync();
   }



   /// <summary>Constructor: validates arguments.</summary>
   [Fact]
   public void Constructor_ValidatesArguments()
   {
      var prompter = new RecordingPrompter();
      Assert.Throws<ArgumentNullException>(() => new UpdateCoordinator(null!, prompter, "x"));
      Assert.Throws<ArgumentNullException>(() => new UpdateCoordinator(_updates, null!, "x"));
      Assert.Throws<ArgumentException>(() => new UpdateCoordinator(_updates, prompter, " "));
   }
}
