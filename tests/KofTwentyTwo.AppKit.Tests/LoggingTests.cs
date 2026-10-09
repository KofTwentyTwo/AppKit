using KofTwentyTwo.AppKit.Logging;


namespace KofTwentyTwo.AppKit.Tests;

public sealed class FileActivityLogTests : IDisposable
{
   private readonly TempDirectory _temp = new();
   private readonly ManualTimeProvider _clock = new(new DateTimeOffset(2026, 3, 15, 9, 30, 0, TimeSpan.Zero));



   public void Dispose() => _temp.Dispose();



   [Fact]
   public void Writes_TimestampedLevels_ToTodaysFile()
   {
      var log = new FileActivityLog(_temp.Path, "app", time: _clock);
      log.Info("hello");
      log.Warning("careful");
      log.Error("boom", new InvalidOperationException("inner detail"));
      log.Error("plain");

      Assert.Equal(_temp.File("app-2026-03-15.log"), log.CurrentLogFilePath);
      string text = File.ReadAllText(log.CurrentLogFilePath);
      Assert.Contains("2026-03-15 09:30:00.000 [INFO] hello", text, StringComparison.Ordinal);
      Assert.Contains("[WARN] careful", text, StringComparison.Ordinal);
      Assert.Contains("[ERROR] boom", text, StringComparison.Ordinal);
      Assert.Contains("inner detail", text, StringComparison.Ordinal);
      Assert.Contains("[ERROR] plain", text, StringComparison.Ordinal);
   }



   [Fact]
   public void RollsOver_AtMidnight()
   {
      var log = new FileActivityLog(_temp.Path, "app", time: _clock);
      log.Info("day one");
      _clock.Now = _clock.Now.AddDays(1);
      log.Info("day two");
      Assert.True(File.Exists(_temp.File("app-2026-03-15.log")));
      Assert.Contains("day two", File.ReadAllText(_temp.File("app-2026-03-16.log")), StringComparison.Ordinal);
   }



   [Fact]
   public void Prunes_FilesOutsideRetention_OncePerDay()
   {
      File.WriteAllText(_temp.File("app-2026-03-13.log"), "kept: inside 3 days");
      File.WriteAllText(_temp.File("app-2026-03-12.log"), "pruned");
      File.WriteAllText(_temp.File("app-notadate.log"), "ignored: not a dated name");
      File.WriteAllText(_temp.File("other-2020-01-01.log"), "ignored: another prefix");

      var log = new FileActivityLog(_temp.Path, "app", retentionDays: 3, time: _clock);
      log.Info("first");
      Assert.False(File.Exists(_temp.File("app-2026-03-12.log")));
      Assert.True(File.Exists(_temp.File("app-2026-03-13.log")));
      Assert.True(File.Exists(_temp.File("app-notadate.log")));
      Assert.True(File.Exists(_temp.File("other-2020-01-01.log")));

      // Already pruned today: a newly stale file survives until tomorrow's first write.
      File.WriteAllText(_temp.File("app-2026-01-01.log"), "stale");
      log.Info("second");
      Assert.True(File.Exists(_temp.File("app-2026-01-01.log")));

      _clock.Now = _clock.Now.AddDays(1);
      log.Info("next day");
      Assert.False(File.Exists(_temp.File("app-2026-01-01.log")));
      Assert.False(File.Exists(_temp.File("app-2026-03-13.log")));
   }



   [Fact]
   public void Prune_SkipsFilesItCannotDelete()
   {
      string locked = _temp.File("app-2020-01-01.log");
      File.WriteAllText(locked, "held open");
      using(new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.None))
      {
         var log = new FileActivityLog(_temp.Path, "app", retentionDays: 1, time: _clock);
         log.Info("still logs");
         Assert.True(File.Exists(log.CurrentLogFilePath));
      }
      Assert.True(File.Exists(locked));
   }



   [Fact]
   public void ZeroRetention_KeepsEverything()
   {
      File.WriteAllText(_temp.File("app-2000-01-01.log"), "ancient");
      var log = new FileActivityLog(_temp.Path, "app", retentionDays: 0, time: _clock);
      log.Info("x");
      Assert.Equal(0, log.RetentionDays);
      Assert.True(File.Exists(_temp.File("app-2000-01-01.log")));
   }



   [Fact]
   public void WriteFailures_AreSwallowed()
   {
      string notADirectory = _temp.File("file");
      File.WriteAllText(notADirectory, "");
      var log = new FileActivityLog(notADirectory, "app", time: _clock);
      log.Info("goes nowhere");
      log.Error("also nowhere", new InvalidOperationException());
      Assert.Equal(notADirectory, log.LogDirectory);
   }



   [Fact]
   public void ForApp_UsesTheAppsLogsFolderAndId()
   {
      var app = new AppInfo { Id = "my-app", DisplayName = "X" };
      var paths = new AppPaths(app, _ => _temp.Path);
      var log = FileActivityLog.ForApp(app, paths);
      Assert.Equal(paths.LogsDirectory, log.LogDirectory);
      Assert.Equal(FileActivityLog.DefaultRetentionDays, log.RetentionDays);
      Assert.StartsWith("my-app-", Path.GetFileName(log.CurrentLogFilePath), StringComparison.Ordinal);
      Assert.Throws<ArgumentNullException>(() => FileActivityLog.ForApp(null!, paths));
      Assert.Throws<ArgumentNullException>(() => FileActivityLog.ForApp(app, null!));
   }



   [Fact]
   public void Constructor_ValidatesArguments()
   {
      Assert.Throws<ArgumentException>(() => new FileActivityLog(" ", "app"));
      Assert.Throws<ArgumentException>(() => new FileActivityLog(_temp.Path, ""));
      Assert.NotNull(new FileActivityLog(_temp.Path, "app").CurrentLogFilePath);
   }



   [Fact]
   public void NullLog_DiscardsEverything()
   {
      NullActivityLog log = NullActivityLog.Instance;
      log.Info("a");
      log.Warning("b");
      log.Error("c", new InvalidOperationException());
      Assert.Equal("", log.LogDirectory);
      Assert.Equal("", log.CurrentLogFilePath);
   }
}



public sealed class LogTailTests : IDisposable
{
   private readonly TempDirectory _temp = new();



   public void Dispose() => _temp.Dispose();



   [Theory]
   [InlineData(null)]
   [InlineData("")]
   [InlineData(@"Z:\does\not\exist.log")]
   public void Read_MissingFile_IsEmpty(string? path)
   {
      LogSnapshot snapshot = LogTail.Read(path);
      Assert.Empty(snapshot.Lines);
      Assert.Equal(0, snapshot.Length);
   }



   [Fact]
   public void Read_KeepsOnlyTheTail()
   {
      string path = _temp.File("a.log");
      File.WriteAllLines(path, ["1", "2", "3", "4"]);
      LogSnapshot snapshot = LogTail.Read(path, maxLines: 2);
      Assert.Equal(["3", "4"], snapshot.Lines);
      Assert.Equal(new FileInfo(path).Length, snapshot.Length);
   }



   [Fact]
   public void Read_LockedFile_ReturnsAnExplanation()
   {
      string path = _temp.File("locked.log");
      File.WriteAllText(path, "x");
      using var hold = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
      LogSnapshot snapshot = LogTail.Read(path);
      Assert.Equal("Could not read the log file.", snapshot.Lines[0]);
      Assert.Equal(0, snapshot.Length);
   }



   [Fact]
   public void Read_RejectsNonPositiveLineCounts()
   {
      Assert.Throws<ArgumentOutOfRangeException>(() => LogTail.Read("x", 0));
   }



   [Fact]
   public void ErrorsOnly_KeepsErrorEntriesWithContinuationLines()
   {
      string[] lines =
      [
          "stray line before any entry",
            "2026-01-01 10:00:00.000 [INFO] fine",
            "2026-01-01 10:00:01.000 [ERROR] broke",
            "System.Exception: detail",
            "   at Somewhere()",
            "2026-01-01 10:00:02.000 [WARN] meh",
        ];
      Assert.Equal(
          ["2026-01-01 10:00:01.000 [ERROR] broke", "System.Exception: detail", "   at Somewhere()"],
          LogTail.ErrorsOnly(lines));
   }



   [Fact]
   public void ToDisplayText_HasFriendlyEmptyStates()
   {
      Assert.Equal("No log entries yet.", LogTail.ToDisplayText([], errorsOnly: false));
      Assert.Equal("No errors logged.", LogTail.ToDisplayText([], errorsOnly: true));
      Assert.Equal("a" + Environment.NewLine + "b", LogTail.ToDisplayText(["a", "b"], errorsOnly: false));
   }



   [Fact]
   public void NullArguments_Throw()
   {
      Assert.Throws<ArgumentNullException>(() => LogTail.ErrorsOnly(null!));
      Assert.Throws<ArgumentNullException>(() => LogTail.ToDisplayText(null!, false));
   }
}
