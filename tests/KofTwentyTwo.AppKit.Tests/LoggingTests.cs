/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Text;
using KofTwentyTwo.AppKit.Logging;


namespace KofTwentyTwo.AppKit.Tests;

/// <summary>Tests for the daily file log: entry format, rollover, retention, and failure handling.</summary>
public sealed class FileActivityLogTests : IDisposable
{
   private readonly TempDirectory _temp = new();
   private readonly ManualTimeProvider _clock = new(new DateTimeOffset(2026, 3, 15, 9, 30, 0, TimeSpan.Zero));



   /// <summary>Deletes the test&apos;s temporary folder.</summary>
   public void Dispose() => _temp.Dispose();



   /// <summary>Writes: timestamped levels to todays file.</summary>
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



   /// <summary>RollsOver: at midnight.</summary>
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



   /// <summary>Prunes: files outside retention once per day.</summary>
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



   /// <summary>Prune: skips files it cannot delete.</summary>
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



   /// <summary>ZeroRetention: keeps everything.</summary>
   [Fact]
   public void ZeroRetention_KeepsEverything()
   {
      File.WriteAllText(_temp.File("app-2000-01-01.log"), "ancient");
      var log = new FileActivityLog(_temp.Path, "app", retentionDays: 0, time: _clock);
      log.Info("x");
      Assert.Equal(0, log.RetentionDays);
      Assert.True(File.Exists(_temp.File("app-2000-01-01.log")));
   }



   /// <summary>WriteFailures: are swallowed.</summary>
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



   /// <summary>A log directory removed between writes is recreated rather than silently dropping diagnostics.</summary>
   [Fact]
   public void Info_RemovedDirectory_RecreatesAndWrites()
   {
      string directory = _temp.File("logs");
      var log = new FileActivityLog(directory, "app", time: _clock);
      log.Info("first");
      Directory.Delete(directory, recursive: true);
      log.Info("recovered");
      Assert.Contains("recovered", File.ReadAllText(log.CurrentLogFilePath), StringComparison.Ordinal);
   }



   /// <summary>ForApp: uses the apps logs folder and id.</summary>
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



   /// <summary>Constructor: validates arguments.</summary>
   [Fact]
   public void Constructor_ValidatesArguments()
   {
      Assert.Throws<ArgumentException>(() => new FileActivityLog(" ", "app"));
      Assert.Throws<ArgumentException>(() => new FileActivityLog(_temp.Path, ""));
      Assert.NotNull(new FileActivityLog(_temp.Path, "app").CurrentLogFilePath);
   }



   /// <summary>NullLog: discards everything.</summary>
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



/// <summary>Tests for reading and filtering log tails.</summary>
public sealed class LogTailTests : IDisposable
{
   private readonly TempDirectory _temp = new();



   /// <summary>Deletes the test&apos;s temporary folder.</summary>
   public void Dispose() => _temp.Dispose();



   /// <summary>Read: missing file is empty.</summary>
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



   /// <summary>Read: keeps only the tail.</summary>
   [Fact]
   public void Read_KeepsOnlyTheTail()
   {
      string path = _temp.File("a.log");
      File.WriteAllLines(path, ["1", "2", "3", "4"]);
      LogSnapshot snapshot = LogTail.Read(path, maxLines: 2);
      Assert.Equal(["3", "4"], snapshot.Lines);
      Assert.Equal(new FileInfo(path).Length, snapshot.Length);
      Assert.False(snapshot.IsTruncated);
   }



   /// <summary>The viewer can read a log while its writer keeps a shared write handle open.</summary>
   [Fact]
   public void Read_WithOpenWriter_ReturnsCurrentTail()
   {
      string path = _temp.File("active.log");
      using var writer = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
      writer.Write(Encoding.UTF8.GetBytes("first\nlatest\n"));
      writer.Flush();
      Assert.Equal(["first", "latest"], LogTail.Read(path).Lines);
   }



   /// <summary>Seeking to the bounded tail preserves the final entries of a huge sparse file.</summary>
   [Fact]
   public void Read_HugeFile_ReturnsOrderedTailWithinDefaultBudget()
   {
      string path = _temp.File("huge.log");
      const long Length = 128 * 1024 * 1024;
      byte[] ending = Encoding.UTF8.GetBytes("\nolder\nlast one\nlast two\n");
      using(var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
      {
         stream.SetLength(Length);
         stream.Position = Length - ending.Length;
         stream.Write(ending);
      }

      LogSnapshot snapshot = LogTail.Read(path, maxLines: 2);
      Assert.Equal(["last one", "last two"], snapshot.Lines);
      Assert.Equal(Length, snapshot.Length);
      Assert.True(snapshot.IsTruncated);
   }



   /// <summary>The byte budget bounds a single line and avoids allocating a queue for an arbitrary line count.</summary>
   [Fact]
   public void Read_HugeSingleLine_BoundsTextAndQueueAllocation()
   {
      string path = _temp.File("one-line.log");
      const int Length = LogTail.DefaultMaxBytes * 4;
      using(var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
      {
         stream.SetLength(Length);
         stream.Position = Length - 1;
         stream.WriteByte((byte)'x');
      }

      LogSnapshot snapshot = LogTail.Read(path, maxLines: int.MaxValue);
      string line = Assert.Single(snapshot.Lines);
      Assert.Equal(LogTail.DefaultMaxBytes, line.Length);
      Assert.EndsWith("x", line, StringComparison.Ordinal);
      Assert.Equal(Length, snapshot.Length);
      Assert.True(snapshot.IsTruncated);
      Assert.Equal(["\0\0\0x"], LogTail.Read(path, maxLines: 1, maxBytes: 4).Lines);
   }



   /// <summary>BOM detection and truncated code-unit alignment preserve UTF-8, UTF-16 and UTF-32 in both byte orders.</summary>
   [Theory]
   [InlineData(0)]
   [InlineData(1)]
   [InlineData(2)]
   [InlineData(3)]
   [InlineData(4)]
   [InlineData(5)]
   public void Read_SupportedEncodings_PreservesTailAndAlignment(int format)
   {
      Encoding encoding = format switch
      {
         0 => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
         1 => new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
         2 => Encoding.Unicode,
         3 => Encoding.BigEndianUnicode,
         4 => Encoding.UTF32,
         _ => new UTF32Encoding(bigEndian: true, byteOrderMark: true),
      };
      string path = _temp.File("encoded.log");
      const string Tail = "one π\ntwo λ\n";
      File.WriteAllText(path, "skip prefix\n" + Tail, encoding);

      LogSnapshot full = LogTail.Read(path);
      Assert.Equal(File.ReadAllLines(path), full.Lines, StringComparer.Ordinal);
      Assert.False(full.IsTruncated);
      LogSnapshot bounded = LogTail.Read(path, maxLines: 2, maxBytes: encoding.GetByteCount(Tail) + 1);
      Assert.Equal(["one π", "two λ"], bounded.Lines);
      Assert.Equal(new FileInfo(path).Length, bounded.Length);
      Assert.True(bounded.IsTruncated);
   }



   /// <summary>Empty files and BOM-only files contain no phantom entries.</summary>
   [Fact]
   public void Read_EmptyOrPreambleOnly_IsEmpty()
   {
      string path = _temp.File("empty.log");
      File.WriteAllBytes(path, []);
      Assert.Empty(LogTail.Read(path).Lines);
      File.WriteAllBytes(path, Encoding.UTF32.GetPreamble());
      LogSnapshot snapshot = LogTail.Read(path);
      Assert.Empty(snapshot.Lines);
      Assert.Equal(4, snapshot.Length);
      Assert.False(snapshot.IsTruncated);
   }



   /// <summary>Read: locked file returns an explanation.</summary>
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



   /// <summary>Read: rejects non positive line counts.</summary>
   [Fact]
   public void Read_RejectsNonPositiveLineCounts()
   {
      Assert.Throws<ArgumentOutOfRangeException>(() => LogTail.Read("x", 0));
      Assert.Throws<ArgumentOutOfRangeException>(() => LogTail.Read("x", 1, 3));
      Assert.Throws<ArgumentOutOfRangeException>(() => LogTail.Read("x", 1, LogTail.DefaultMaxBytes + 1));
   }



   /// <summary>ErrorsOnly: keeps error entries with continuation lines.</summary>
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



   /// <summary>ToDisplayText: has friendly empty states.</summary>
   [Fact]
   public void ToDisplayText_HasFriendlyEmptyStates()
   {
      Assert.Equal("No log entries yet.", LogTail.ToDisplayText([], errorsOnly: false));
      Assert.Equal("No errors logged.", LogTail.ToDisplayText([], errorsOnly: true));
      Assert.Equal("a" + Environment.NewLine + "b", LogTail.ToDisplayText(["a", "b"], errorsOnly: false));
   }



   /// <summary>Both viewer modes disclose when the byte limit omitted input, including a partial first line.</summary>
   [Theory]
   [InlineData(false, "tail")]
   [InlineData(true, "No errors logged.")]
   public void ToDisplayText_TruncatedInput_ShowsNotice(bool errorsOnly, string expected)
   {
      string text = LogTail.ToDisplayText(errorsOnly ? [] : ["tail"], errorsOnly, isTruncated: true);
      Assert.StartsWith("Showing a bounded log tail;", text, StringComparison.Ordinal);
      Assert.EndsWith(expected, text, StringComparison.Ordinal);
   }



   /// <summary>NullArguments: throw.</summary>
   [Fact]
   public void NullArguments_Throw()
   {
      Assert.Throws<ArgumentNullException>(() => LogTail.ErrorsOnly(null!));
      Assert.Throws<ArgumentNullException>(() => LogTail.ToDisplayText(null!, false));
   }
}
