/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Updates;
using Microsoft.UI.Dispatching;


namespace AppKit.Sample.WinUI;

/// <summary>
/// Custom entry point (DISABLE_XAML_GENERATED_MAIN) so Velopack handles its install,
/// update, and uninstall hooks before any UI exists.
/// </summary>
public static class Program
{
   /// <summary>Runs Velopack&apos;s install and update hooks first, then starts the WinUI application (the XAML-generated Main is disabled for this).</summary>
   [STAThread]
   private static void Main()
   {
      VelopackStartup.Run();

      // The remainder mirrors the XAML-generated Main.
      WinRT.ComWrappersSupport.InitializeComWrappers();
      Microsoft.UI.Xaml.Application.Start(callbackParams =>
      {
         SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
         _ = new App();
      });
   }
}
