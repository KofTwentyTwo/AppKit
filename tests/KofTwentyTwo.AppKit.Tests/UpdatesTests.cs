using KofTwentyTwo.AppKit.Updates;


namespace KofTwentyTwo.AppKit.Tests;

public class UpdateCheckResultTests
{
   [Fact]
   public void Factories()
   {
      Assert.False(UpdateCheckResult.UpToDate.IsUpdateAvailable);
      Assert.True(UpdateCheckResult.Available("2.0.0").IsUpdateAvailable);
      Assert.False(UpdateCheckResult.Failed("x").IsUpdateAvailable);
      Assert.False(new UpdateCheckResult("2.0.0", "but failed").IsUpdateAvailable);
   }
}



public class VelopackUpdateServiceTests
{
   [Fact]
   public async Task NotInstalled_IsUnsupported()
   {
      var service = new VelopackUpdateService(() => new FakeBackend { IsInstalled = false, CurrentVersion = null });
      Assert.False(service.IsSupported);
      Assert.Null(service.CurrentVersion);
      UpdateCheckResult result = await service.CheckAsync();
      Assert.Equal(UpdateText.NotSupported, result.Error);
   }



   [Fact]
   public async Task BackendThatCannotBeCreated_IsUnsupportedAndNeverThrows()
   {
      var service = new VelopackUpdateService(() => throw new InvalidOperationException("no manager"));
      Assert.False(service.IsSupported);
      Assert.Null(service.CurrentVersion);
      Assert.Equal("no manager", (await service.CheckAsync()).Error);
      Assert.NotNull(await service.DownloadAndApplyAsync());
   }



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



   [Fact]
   public async Task Check_UpToDate_LeavesNothingToInstall()
   {
      var service = new VelopackUpdateService(() => new FakeBackend());
      Assert.Same(UpdateCheckResult.UpToDate, await service.CheckAsync());
      Assert.Equal("No update is ready to install; check for updates first.", await service.DownloadAndApplyAsync());
   }



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



   [Fact]
   public async Task DownloadFailure_IsReturned()
   {
      var backend = new FakeBackend { Available = "2.0.0", DownloadFailure = new IOException("disk full") };
      var service = new VelopackUpdateService(() => backend);
      await service.CheckAsync();
      Assert.Equal("disk full", await service.DownloadAndApplyAsync());
      Assert.Equal(0, backend.Applies);
   }



   [Fact]
   public async Task RealVelopack_OutsideAnInstall_IsUnsupported()
   {
      var service = new VelopackUpdateService(new Uri("https://github.com/KofTwentyTwo/AppKit"));
      Assert.False(service.IsSupported);
      Assert.Null(service.CurrentVersion);
      Assert.NotNull((await service.CheckAsync()).Error);
      Assert.False(new VelopackUpdateService(new Uri("https://github.com/KofTwentyTwo/AppKit"), includePrereleases: true).IsSupported);
   }



   [Fact]
   public void NullRepository_Throws()
   {
      Assert.Throws<ArgumentNullException>(() => new VelopackUpdateService(null!, includePrereleases: false));
   }



   private sealed class SynchronousProgress(Action<int> report) : IProgress<int>
   {
      public void Report(int value) => report(value);
   }
}



public class UpdateCoordinatorTests
{
   private readonly FakeUpdateService _updates = new();
   private readonly RecordingLog _log = new();



   private UpdateCoordinator Coordinator(RecordingPrompter prompter) => new(_updates, prompter, "Sample", _log);



   [Fact]
   public async Task Interactive_Unsupported_SaysSo()
   {
      _updates.IsSupported = false;
      var prompter = new RecordingPrompter();
      await Coordinator(prompter).CheckInteractivelyAsync();
      Assert.Equal((UpdateText.CheckTitle, UpdateText.NotSupported), prompter.Messages.Single());
   }



   [Fact]
   public async Task Interactive_Failure_ExplainsAndLogs()
   {
      _updates.NextCheck = UpdateCheckResult.Failed("offline");
      var prompter = new RecordingPrompter();
      await Coordinator(prompter).CheckInteractivelyAsync();
      Assert.Equal("Could not check for updates.\noffline", prompter.Messages.Single().Message);
      Assert.Equal("WARN Update check failed: offline", _log.Entries.Single());
   }



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



   [Fact]
   public async Task NullLog_IsAllowed()
   {
      _updates.NextCheck = UpdateCheckResult.Failed("offline");
      await new UpdateCoordinator(_updates, new RecordingPrompter(), "Sample").CheckQuietlyAsync();
   }



   [Fact]
   public void Constructor_ValidatesArguments()
   {
      var prompter = new RecordingPrompter();
      Assert.Throws<ArgumentNullException>(() => new UpdateCoordinator(null!, prompter, "x"));
      Assert.Throws<ArgumentNullException>(() => new UpdateCoordinator(_updates, null!, "x"));
      Assert.Throws<ArgumentException>(() => new UpdateCoordinator(_updates, prompter, " "));
   }
}
