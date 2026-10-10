/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Logging;
using KofTwentyTwo.AppKit.WinUI;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;


namespace AppKit.Sample.WinUI;

/// <summary>The WinUI sample application: installs AppKit&apos;s crash net and the saved theme, then opens the main window.</summary>
public partial class App : Application
{
   private Window? _window;

   /// <summary>The app-wide activity log.</summary>
   internal static IActivityLog Log { get; } = FileActivityLog.ForApp(SampleApp.Info, SampleApp.Paths);

   /// <summary>The shared structured logger, including scopes.</summary>
   internal static ILogger Logger { get; } = Log.AsLogger();



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
      LogStarted(Logger, SampleApp.Info.DisplayName);
      _window = new MainWindow();
      _window.Activate();
   }



   /// <summary>Records app identity as a field without formatting it into the template.</summary>
   [LoggerMessage(100, LogLevel.Information, "{AppName} started.")]
   private static partial void LogStarted(ILogger logger, string appName);
}
