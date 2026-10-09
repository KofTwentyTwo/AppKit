/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Logging;
using KofTwentyTwo.AppKit.WinUI;
using Microsoft.UI.Xaml;


namespace AppKit.Sample.WinUI;

public partial class App : Application
{
   private Window? _window;

   /// <summary>The app-wide activity log.</summary>
   internal static IActivityLog Log { get; } = FileActivityLog.ForApp(SampleApp.Info, SampleApp.Paths);



   public App()
   {
      InitializeComponent();
      WinUIShell.InstallCrashNet(this, () => Log);
      // Only settable here, before any window exists.
      WinUIShell.ApplyTheme(this, SampleApp.SettingsStore.Load().ThemeKind);
   }



   protected override void OnLaunched(LaunchActivatedEventArgs args)
   {
      Log.Info($"{SampleApp.Info.DisplayName} started.");
      _window = new MainWindow();
      _window.Activate();
   }
}
