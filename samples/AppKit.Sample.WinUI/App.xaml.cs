/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Logging;
using KofTwentyTwo.AppKit.WinUI;
using Microsoft.UI.Xaml;


namespace AppKit.Sample.WinUI;

/// <summary>The WinUI sample application: installs AppKit&apos;s crash net and the saved theme, then opens the main window.</summary>
public partial class App : Application
{
   private Window? _window;

   /// <summary>The app-wide activity log.</summary>
   internal static IActivityLog Log { get; } = FileActivityLog.ForApp(SampleApp.Info, SampleApp.Paths);



   /// <summary>Installs the crash net and applies the saved theme; WinUI allows setting the application theme only here, before any window exists.</summary>
   public App()
   {
      InitializeComponent();
      WinUIShell.InstallCrashNet(this, () => Log);
      // Only settable here, before any window exists.
      WinUIShell.ApplyTheme(this, SampleApp.SettingsStore.Load().ThemeKind);
   }



   /// <summary>Logs the start and opens the main window.</summary>
   protected override void OnLaunched(LaunchActivatedEventArgs args)
   {
      Log.Info($"{SampleApp.Info.DisplayName} started.");
      _window = new MainWindow();
      _window.Activate();
   }
}
