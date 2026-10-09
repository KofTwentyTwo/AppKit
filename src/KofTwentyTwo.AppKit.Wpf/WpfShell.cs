using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using KofTwentyTwo.AppKit.Interaction;
using KofTwentyTwo.AppKit.Logging;
using KofTwentyTwo.AppKit.Settings;


namespace KofTwentyTwo.AppKit.Wpf;

/// <summary>App-level wiring every WPF app repeats: crash net and theme.</summary>
public static class WpfShell
{
   /// <summary>
   /// Last-resort net: records exceptions that escape UI event handlers and keeps the
   /// app running, and records unobserved task exceptions. Call from App startup.
   /// </summary>
   /// <param name="application">The app.</param>
   /// <param name="log">Supplies the log; may return null early in startup.</param>
   public static void InstallCrashNet(Application application, Func<IActivityLog?> log)
   {
      ArgumentNullException.ThrowIfNull(application);
      ArgumentNullException.ThrowIfNull(log);
      application.DispatcherUnhandledException += (_, e) =>
      {
         e.Handled = true;
         SafeLog(log, "Unhandled UI exception", e.Exception);
      };
      TaskScheduler.UnobservedTaskException += (_, e) =>
      {
         e.SetObserved();
         SafeLog(log, "Unobserved task exception", e.Exception);
      };
   }



   /// <summary>
   /// Applies the Windows 11 Fluent theme in the requested mode. Safe to call at any
   /// time; WPF restyles open windows live.
   /// </summary>
   public static void ApplyTheme(Application application, AppTheme theme)
   {
      ArgumentNullException.ThrowIfNull(application);
      application.ThemeMode = theme switch
      {
         AppTheme.Light => ThemeMode.Light,
         AppTheme.Dark => ThemeMode.Dark,
         _ => ThemeMode.System,
      };
   }



   /// <summary>Sets the brand icon on a window. A missing .ico is ignored.</summary>
   public static void SetAppIcon(this Window window, AppInfo app)
   {
      ArgumentNullException.ThrowIfNull(window);
      ArgumentNullException.ThrowIfNull(app);
      string path = Path.Combine(AppContext.BaseDirectory, app.Brand.IconIcoPath);
      if(File.Exists(path))
      {
         window.Icon = BitmapFrame.Create(new Uri(path));
      }
   }



   private static void SafeLog(Func<IActivityLog?> log, string message, Exception exception)
   {
      try
      {
         log()?.Error($"{message}: {exception.Message}", exception);
      }
      catch
      {
         // Logging must never turn a survivable error fatal.
      }
   }
}



/// <summary>
/// <see cref="IUserPrompter"/> over WPF message boxes, for shared flows such as
/// <see cref="Updates.UpdateCoordinator"/>. Message boxes cannot relabel their buttons,
/// so confirmations show Yes/No with the confirm text folded into the question.
/// </summary>
/// <param name="owner">Supplies the owning window, usually the main window.</param>
public sealed class MessageBoxPrompter(Func<Window?> owner) : IUserPrompter
{
   /// <inheritdoc/>
   public Task ShowMessageAsync(string title, string message)
   {
      Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
      return Task.CompletedTask;
   }



   /// <inheritdoc/>
   public Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText)
   {
      MessageBoxResult result = Show($"{message}\n\n{confirmText}?", title, MessageBoxButton.YesNo, MessageBoxImage.Question);
      return Task.FromResult(result == MessageBoxResult.Yes);
   }



   private MessageBoxResult Show(string message, string title, MessageBoxButton buttons, MessageBoxImage image)
       => owner() is Window window
           ? MessageBox.Show(window, message, title, buttons, image)
           : MessageBox.Show(message, title, buttons, image);
}
