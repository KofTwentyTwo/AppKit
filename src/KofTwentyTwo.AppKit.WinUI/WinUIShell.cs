/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Logging;
using KofTwentyTwo.AppKit.Settings;
using Microsoft.UI.Xaml;


namespace KofTwentyTwo.AppKit.WinUI;

/// <summary>App-level wiring every WinUI app repeats: crash net and theme.</summary>
public static class WinUIShell
{
   /// <summary>
   /// Last-resort net: an exception escaping an async void event handler is rethrown
   /// on the dispatcher with no frame above it, and the process dies as a stowed
   /// exception (0xc000027b) with nothing in the log. This records it and keeps the app
   /// running. Call from the App constructor.
   /// </summary>
   /// <param name="application">The app.</param>
   /// <param name="log">Supplies the log; may return null early in startup.</param>
   public static void InstallCrashNet(Application application, Func<IActivityLog?> log)
   {
      ArgumentNullException.ThrowIfNull(application);
      ArgumentNullException.ThrowIfNull(log);
      application.UnhandledException += (_, e) =>
      {
         e.Handled = true;
         try
         {
            log()?.Error($"Unhandled UI exception: {e.Message}", e.Exception);
         }
         catch
         {
            // Logging must never turn a survivable error fatal; the stowed
            // Exception property itself can throw when type info was lost.
         }
      };
   }



   /// <summary>
   /// Applies the startup theme. Only valid in the App constructor, before any window
   /// exists; for live changes use <see cref="ApplyTheme(FrameworkElement, AppTheme)"/>.
   /// </summary>
   public static void ApplyTheme(Application application, AppTheme theme)
   {
      ArgumentNullException.ThrowIfNull(application);
      if(theme == AppTheme.Light)
      {
         application.RequestedTheme = ApplicationTheme.Light;
      }
      else if(theme == AppTheme.Dark)
      {
         application.RequestedTheme = ApplicationTheme.Dark;
      }
   }



   /// <summary>Applies a theme change to a window's root element while the app runs.</summary>
   public static void ApplyTheme(FrameworkElement root, AppTheme theme)
   {
      ArgumentNullException.ThrowIfNull(root);
      root.RequestedTheme = theme switch
      {
         AppTheme.Light => ElementTheme.Light,
         AppTheme.Dark => ElementTheme.Dark,
         _ => ElementTheme.Default,
      };
   }
}
