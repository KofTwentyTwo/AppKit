/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json;
using FsCheck.Xunit;
using KofTwentyTwo.AppKit.Logging;
using KofTwentyTwo.AppKit.Settings;


namespace KofTwentyTwo.AppKit.Tests;

/// <summary>Generates hostile settings and log inputs; FsCheck shrinks and reports a replay seed on failure.</summary>
public sealed class InputProperties
{
   /// <summary>Repair always produces a valid theme and bounded splash, without changing startup choices.</summary>
   [Property(MaxTest = 500, EndSize = 256)]
   public void Sanitize_ArbitraryValues_IsValidAndIdempotent(string? theme, int splash, bool showSplash, bool checkUpdates)
   {
      int[] durations = [int.MinValue, int.MaxValue, 0, splash];
      foreach(int duration in durations)
      {
         var settings = new ShellSettings
         {
            Theme = theme!,
            SplashMilliseconds = duration,
            ShowSplashScreen = showSplash,
            CheckForUpdatesOnStartup = checkUpdates,
         };
         settings.Sanitize();
         AssertValid(settings);
         string repairedTheme = settings.Theme;
         int repairedDuration = settings.SplashMilliseconds;
         settings.Sanitize();
         Assert.Equal(repairedTheme, settings.Theme);
         Assert.Equal(repairedDuration, settings.SplashMilliseconds);
         Assert.Equal(showSplash, settings.ShowSplashScreen);
         Assert.Equal(checkUpdates, settings.CheckForUpdatesOnStartup);
      }
   }



   /// <summary>Arbitrary bytes, including broken UTF-8 and JSON, cannot stop loading preferences.</summary>
   [Property(MaxTest = 500, EndSize = 256)]
   public void Load_ArbitraryBytes_ReturnsUsableSettings(byte[] bytes)
   {
      using var temporary = new TempDirectory();
      string path = temporary.File("settings.json");
      File.WriteAllBytes(path, bytes);
      var store = new SettingsStore<SampleSettings>(path, TestSettingsContext.Default.SampleSettings);
      SampleSettings loaded = store.Load();
      AssertValid(loaded);
      Assert.InRange(loaded.Concurrency, 1, 64);
   }



   /// <summary>Valid JSON with arbitrary values is repaired, and repaired preferences survive a save/load cycle.</summary>
   [Property(MaxTest = 500, EndSize = 256)]
   public void Load_ArbitraryJsonValues_RepairsAndRoundTrips(string? theme, int splash, int concurrency, bool showSplash, bool checkUpdates)
   {
      using var temporary = new TempDirectory();
      string path = temporary.File("settings.json");
      var settings = new SampleSettings
      {
         Theme = theme!,
         SplashMilliseconds = unchecked(splash * 1_000_003),
         Concurrency = concurrency,
         ShowSplashScreen = showSplash,
         CheckForUpdatesOnStartup = checkUpdates,
      };
      File.WriteAllText(path, JsonSerializer.Serialize(settings, TestSettingsContext.Default.SampleSettings));
      var store = new SettingsStore<SampleSettings>(path, TestSettingsContext.Default.SampleSettings);
      SampleSettings loaded = store.Load();
      AssertValid(loaded);
      Assert.InRange(loaded.Concurrency, 1, 64);
      Assert.Equal(showSplash, loaded.ShowSplashScreen);
      Assert.Equal(checkUpdates, loaded.CheckForUpdatesOnStartup);
      Assert.True(store.Save(loaded));
      SampleSettings roundTripped = store.Load();
      Assert.Equal(loaded.Theme, roundTripped.Theme);
      Assert.Equal(loaded.SplashMilliseconds, roundTripped.SplashMilliseconds);
      Assert.Equal(loaded.Concurrency, roundTripped.Concurrency);
      Assert.Equal(showSplash, roundTripped.ShowSplashScreen);
      Assert.Equal(checkUpdates, roundTripped.CheckForUpdatesOnStartup);
      Assert.False(File.Exists(path + ".tmp"));
   }



   /// <summary>Malformed log bytes still yield the correct bounded tail, in original order.</summary>
   [Property(MaxTest = 500, EndSize = 256)]
   public void Read_ArbitraryLogBytes_ReturnsTheBoundedTail(byte[] bytes, byte limit)
   {
      using var temporary = new TempDirectory();
      string path = temporary.File("input.log");
      File.WriteAllBytes(path, bytes);
      int maxLines = limit + 1;
      LogSnapshot snapshot = LogTail.Read(path, maxLines);
      Assert.InRange(snapshot.Lines.Count, 0, maxLines);
      Assert.Equal(bytes.LongLength, snapshot.Length);
      Assert.Equal(File.ReadAllLines(path).TakeLast(maxLines), snapshot.Lines, StringComparer.Ordinal);
   }



   /// <summary>Checks the public settings contract independently of the repair implementation.</summary>
   private static void AssertValid(ShellSettings settings)
   {
      Assert.Contains(settings.Theme, new[] { "System", "Light", "Dark" }, StringComparer.Ordinal);
      Assert.InRange(settings.SplashMilliseconds, ShellSettings.MinSplashMilliseconds, ShellSettings.MaxSplashMilliseconds);
   }
}
