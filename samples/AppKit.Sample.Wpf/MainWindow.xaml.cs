/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Windows;
using System.Windows.Controls;
using KofTwentyTwo.AppKit.Updates;
using KofTwentyTwo.AppKit.Wpf;


namespace AppKit.Sample.Wpf;

public partial class MainWindow : Window
{
   private readonly SampleSettings _settings = SampleApp.SettingsStore.Load();
   private readonly UpdateCoordinator _updates;



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



   private void Exit_Click(object sender, RoutedEventArgs e) => Close();



   private void Theme_Click(object sender, RoutedEventArgs e)
   {
      _settings.Theme = (string)((FrameworkElement)sender).Tag;
      WpfShell.ApplyTheme(Application.Current, _settings.ThemeKind);
      CheckTheme();
      Save();
   }



   private void Splash_Click(object sender, RoutedEventArgs e)
   {
      _settings.ShowSplashScreen = SplashToggle.IsChecked;
      Save();
   }



   private async void CheckForUpdates_Click(object sender, RoutedEventArgs e) => await _updates.CheckInteractivelyAsync();



   private void ActivityLog_Click(object sender, RoutedEventArgs e) => LogWindow.ShowSingle(Program.Log, SampleApp.Info);



   private void About_Click(object sender, RoutedEventArgs e) => AboutWindow.Show(SampleApp.Info, this);



   private void LogInfo_Click(object sender, RoutedEventArgs e)
   {
      Program.Log.Info("The user pressed 'Write a log entry'.");
      StatusText.Text = "Wrote an INFO entry.";
   }



   private void LogError_Click(object sender, RoutedEventArgs e)
   {
      try
      {
         throw new InvalidOperationException("A deliberately handled sample failure.");
      }
      catch(InvalidOperationException ex)
      {
         Program.Log.Error("Sample operation failed.", ex);
         StatusText.Text = "Wrote an ERROR entry with its stack trace.";
      }
   }



   private void Throw_Click(object sender, RoutedEventArgs e)
   {
      StatusText.Text = "Threw; the crash net logged it and the app kept running.";
      throw new InvalidOperationException("Deliberately unhandled sample exception.");
   }



   private void CheckTheme()
   {
      foreach(MenuItem item in new[] { ThemeSystem, ThemeLight, ThemeDark })
      {
         item.IsChecked = string.Equals((string)item.Tag, _settings.Theme, StringComparison.Ordinal);
      }
   }



   private void Save()
   {
      if(!SampleApp.SettingsStore.Save(_settings))
      {
         StatusText.Text = "Could not save settings.";
      }
   }
}
