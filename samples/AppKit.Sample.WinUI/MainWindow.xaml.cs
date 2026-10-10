/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Settings;
using KofTwentyTwo.AppKit.Updates;
using KofTwentyTwo.AppKit.WinUI;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;


namespace AppKit.Sample.WinUI;

/// <summary>The sample&apos;s main window: each control exercises one AppKit service.</summary>
public sealed partial class MainWindow : Window
{
   private readonly SampleSettings _settings = SampleApp.SettingsStore.Load();
   private readonly UpdateCoordinator _updates;



   /// <summary>Sizes and brands the window, wires the update coordinator, restores the saved settings, shows the splash, and schedules the quiet startup update check.</summary>
   public MainWindow()
   {
      InitializeComponent();
      Title = SampleApp.Info.DisplayName;
      this.ResizeForDpi(900, 600);
      this.SetMinimumSize(640, 420);
      this.SetAppIcon(SampleApp.Info);

      _updates = new UpdateCoordinator(
          new VelopackUpdateService(SampleApp.Info.RepositoryUrl!),
          new ContentDialogPrompter(() => Content.XamlRoot),
          SampleApp.Info.DisplayName,
          App.Log);

      GreetingText.Text = _settings.Greeting;
      SplashToggle.IsChecked = _settings.ShowSplashScreen;
      (_settings.ThemeKind switch { AppTheme.Light => ThemeLight, AppTheme.Dark => ThemeDark, _ => ThemeSystem }).IsChecked = true;

      SplashOverlay.ShowOver(RootGrid, SampleApp.Info, _settings);
      Closed += (_, _) => LogWindow.CloseCurrent(); // a log-only process would linger otherwise

      if(_settings.CheckForUpdatesOnStartup)
      {
         // The constructor runs on the UI thread, so the check resumes there to show dialogs.
         _ = CheckForUpdatesAfterStartupAsync();
      }
   }



   /// <summary>
   /// Waits for the splash to clear (dialogs need the window's XamlRoot), then runs the
   /// quiet startup update check, which only speaks up when an update exists.
   /// </summary>
   private async Task CheckForUpdatesAfterStartupAsync()
   {
      await Task.Delay(_settings.ShowSplashScreen ? _settings.SplashMilliseconds + 500 : 1000);
      await _updates.CheckQuietlyAsync();
   }



   /// <summary>File, Exit.</summary>
   private void Exit_Click(object sender, RoutedEventArgs e) => Close();



   /// <summary>View, theme: applies the chosen theme to the window and saves it.</summary>
   private void Theme_Click(object sender, RoutedEventArgs e)
   {
      _settings.Theme = (string)((FrameworkElement)sender).Tag;
      WinUIShell.ApplyTheme(RootGrid, _settings.ThemeKind);
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
   private void ActivityLog_Click(object sender, RoutedEventArgs e) => LogWindow.ShowSingle(App.Log, SampleApp.Info);



   /// <summary>Help, About: shows the About dialog.</summary>
   private async void About_Click(object sender, RoutedEventArgs e) => await AboutDialog.ShowAsync(SampleApp.Info, Content.XamlRoot);



   /// <summary>Writes an INFO entry, to show logging.</summary>
   private void LogInfo_Click(object sender, RoutedEventArgs e)
   {
      LogUserAction(App.Logger);
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
         LogSampleFailure(App.Logger, ex);
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



   /// <summary>Saves the settings and reports a failed save in the status line.</summary>
   private void Save()
   {
      if(!SampleApp.SettingsStore.Save(_settings))
      {
         StatusText.Text = "Could not save settings.";
      }
   }
}
