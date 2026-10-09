/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
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



   /// <summary>Records an exception caught by the crash net; a failure of the logger itself is swallowed so logging can never make an error fatal.</summary>
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
