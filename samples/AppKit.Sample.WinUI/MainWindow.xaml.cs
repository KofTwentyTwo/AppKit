using KofTwentyTwo.AppKit.Settings;
using KofTwentyTwo.AppKit.Updates;
using KofTwentyTwo.AppKit.WinUI;
using Microsoft.UI.Xaml;


namespace AppKit.Sample.WinUI;

public sealed partial class MainWindow : Window
{
   private readonly SampleSettings _settings = SampleApp.SettingsStore.Load();
   private readonly UpdateCoordinator _updates;



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
         DispatcherQueue.TryEnqueue(async () =>
         {
            await Task.Delay(_settings.ShowSplashScreen ? _settings.SplashMilliseconds + 500 : 1000);
            await _updates.CheckQuietlyAsync();
         });
      }
   }



   private void Exit_Click(object sender, RoutedEventArgs e) => Close();



   private void Theme_Click(object sender, RoutedEventArgs e)
   {
      _settings.Theme = (string)((FrameworkElement)sender).Tag;
      WinUIShell.ApplyTheme(RootGrid, _settings.ThemeKind);
      Save();
   }



   private void Splash_Click(object sender, RoutedEventArgs e)
   {
      _settings.ShowSplashScreen = SplashToggle.IsChecked;
      Save();
   }



   private async void CheckForUpdates_Click(object sender, RoutedEventArgs e) => await _updates.CheckInteractivelyAsync();



   private void ActivityLog_Click(object sender, RoutedEventArgs e) => LogWindow.ShowSingle(App.Log, SampleApp.Info);



   private async void About_Click(object sender, RoutedEventArgs e) => await AboutDialog.ShowAsync(SampleApp.Info, Content.XamlRoot);



   private void LogInfo_Click(object sender, RoutedEventArgs e)
   {
      App.Log.Info("The user pressed 'Write a log entry'.");
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
         App.Log.Error("Sample operation failed.", ex);
         StatusText.Text = "Wrote an ERROR entry with its stack trace.";
      }
   }



   private void Throw_Click(object sender, RoutedEventArgs e)
   {
      StatusText.Text = "Threw; the crash net logged it and the app kept running.";
      throw new InvalidOperationException("Deliberately unhandled sample exception.");
   }



   private void Save()
   {
      if(!SampleApp.SettingsStore.Save(_settings))
      {
         StatusText.Text = "Could not save settings.";
      }
   }
}
