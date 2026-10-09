/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Windows;
using KofTwentyTwo.AppKit.Logging;
using KofTwentyTwo.AppKit.Updates;
using KofTwentyTwo.AppKit.Wpf;


namespace AppKit.Sample.Wpf;

/// <summary>Entry point: Velopack hooks first, then the WPF application.</summary>
public static class Program
{
   /// <summary>The app-wide activity log.</summary>
   internal static IActivityLog Log { get; } = FileActivityLog.ForApp(SampleApp.Info, SampleApp.Paths);



   /// <summary>Runs Velopack&apos;s install and update hooks first, then creates the WPF application with AppKit&apos;s crash net and theme and shows the main window.</summary>
   [STAThread]
   private static void Main()
   {
      VelopackStartup.Run();

      var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
      WpfShell.InstallCrashNet(app, () => Log);
      WpfShell.ApplyTheme(app, SampleApp.SettingsStore.Load().ThemeKind);
      Log.Info($"{SampleApp.Info.DisplayName} (WPF) started.");
      app.Run(new MainWindow());
   }
}
