/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit;
using KofTwentyTwo.AppKit.Settings;


namespace AppKit.Sample;

/// <summary>Identity and settings shared by both sample apps (linked into each project).</summary>
internal static class SampleApp
{
   public static AppInfo Info { get; } = new()
   {
      Id = "appkit-sample",
      DisplayName = "AppKit Sample",
      Tagline = "Everything a KofTwentyTwo Windows app gets for free",
      Description = "A minimal app wired to every AppKit service: splash, About, activity log, settings, theme, crash net, and Velopack self-update.",
      RepositoryUrl = new Uri("https://github.com/KofTwentyTwo/AppKit"),
      Author = "James Maes",
      AuthorEmail = "james@kof22.com",
      Website = new Uri("https://kof22.com"),
      Copyright = "Copyright (c) 2026 James Maes",
      Brand = new AppBrand { IconSvgPath = "Assets/Brand/app-icon.svg", IconIcoPath = "Assets/Brand/app.ico" },
      Attributions =
      [
         new("KofTwentyTwo.AppKit", "MIT License"),
         new("Velopack", "MIT License"),
         new(".NET Runtime", "MIT License"),
      ],
   };



   public static AppPaths Paths { get; } = new(Info);

   public static SettingsStore<SampleSettings> SettingsStore { get; } = new(Paths.SettingsFile, SampleSettingsContext.Default.SampleSettings);
}
