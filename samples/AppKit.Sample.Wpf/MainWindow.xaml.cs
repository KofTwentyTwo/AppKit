/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Windows;
using System.Windows.Controls;
using KofTwentyTwo.AppKit.Updates;
using KofTwentyTwo.AppKit.Wpf;
using Microsoft.Extensions.Logging;


namespace AppKit.Sample.Wpf;

/// <summary>The sample&apos;s main window: each control exercises one AppKit service.</summary>
public partial class MainWindow : Window
{
   private readonly SampleSettings _settings = SampleApp.SettingsStore.Load();
   private readonly UpdateCoordinator _updates;



   /// <summary>Brands the window, wires the update coordinator, restores the saved settings, shows the splash, and schedules the quiet startup update check.</summary>
   public MainWindow()
   {
      InitializeComponent();
      Title = SampleApp.Info.DisplayName;
      this.SetAppIcon(SampleApp.Info);

      _updates = new UpdateCoordinator(
          new VelopackUpdateService(SampleApp.Info.RepositoryUrl!),
          new MessageBoxPrompter(() => this),
          SampleApp.Info.DisplayName,
          Program.Log);

      GreetingText.Text = _settings.Greeting;
      SplashToggle.IsChecked = _settings.ShowSplashScreen;
      CheckTheme();

      SplashOverlay.ShowOver(RootGrid, SampleApp.Info, _settings);
      Closed += (_, _) => LogWindow.CloseCurrent();

      if(_settings.CheckForUpdatesOnStartup)
      {
         Loaded += async (_, _) =>
         {
            await Task.Delay(_settings.ShowSplashScreen ? _settings.SplashMilliseconds + 500 : 1000);
            await _updates.CheckQuietlyAsync();
         };
      }
   }



   /// <summary>File, Exit.</summary>
   private void Exit_Click(object sender, RoutedEventArgs e) => Close();



   /// <summary>View, theme: applies the chosen Fluent theme to the application and saves it.</summary>
   private void Theme_Click(object sender, RoutedEventArgs e)
   {
      _settings.Theme = (string)((FrameworkElement)sender).Tag;
      WpfShell.ApplyTheme(Application.Current, _settings.ThemeKind);
      CheckTheme();
      Save();
   }



   /// <summary>View, Show splash at startup: saves the preference.</summary>
   private void Splash_Click(object sender, RoutedEventArgs e)
   {
      _settings.ShowSplashScreen = SplashToggle.IsChecked;
      Save();
   }



   /// <summary>Help, Check for updates: runs the interactive update flow.</summary>
   private async void CheckForUpdates_Click(object sender, RoutedEventArgs e) => await _updates.CheckInteractivelyAsync();



   /// <summary>Help, Activity log: opens the live log window, or brings the open one forward.</summary>
   private void ActivityLog_Click(object sender, RoutedEventArgs e) => LogWindow.ShowSingle(Program.Log, SampleApp.Info);



   /// <summary>Help, About: shows the About window.</summary>
   private void About_Click(object sender, RoutedEventArgs e) => AboutWindow.Show(SampleApp.Info, this);



   /// <summary>Writes an INFO entry, to show logging.</summary>
   private void LogInfo_Click(object sender, RoutedEventArgs e)
   {
      LogUserAction(Program.Logger);
      StatusText.Text = "Wrote an INFO entry.";
   }



   /// <summary>Logs a handled exception with its stack trace, to show error logging.</summary>
   private void LogError_Click(object sender, RoutedEventArgs e)
   {
      try
      {
         throw new InvalidOperationException("A deliberately handled sample failure.");
      }
      catch(InvalidOperationException ex)
      {
         LogSampleFailure(Program.Logger, ex);
         StatusText.Text = "Wrote an ERROR entry with its stack trace.";
      }
   }



   /// <summary>Records the sample button interaction.</summary>
   [LoggerMessage(101, LogLevel.Information, "The user pressed 'Write a log entry'.")]
   private static partial void LogUserAction(ILogger logger);



   /// <summary>Records a handled sample failure with its full exception.</summary>
   [LoggerMessage(102, LogLevel.Error, "Sample operation failed.")]
   private static partial void LogSampleFailure(ILogger logger, Exception exception);



   /// <summary>Throws on purpose, to show that the crash net logs the exception and the app keeps running.</summary>
   private void Throw_Click(object sender, RoutedEventArgs e)
   {
      StatusText.Text = "Threw; the crash net logged it and the app kept running.";
      throw new InvalidOperationException("Deliberately unhandled sample exception.");
   }



   /// <summary>Checks the View menu item that matches the saved theme and clears the others.</summary>
   private void CheckTheme()
   {
      foreach(MenuItem item in new[] { ThemeSystem, ThemeLight, ThemeDark })
      {
         item.IsChecked = string.Equals((string)item.Tag, _settings.Theme, StringComparison.Ordinal);
      }
   }



   /// <summary>Saves the settings and reports a failed save in the status line.</summary>
   private void Save()
   {
      if(!SampleApp.SettingsStore.Save(_settings))
      {
         StatusText.Text = "Could not save settings.";
      }
   }
}
