/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;


namespace KofTwentyTwo.AppKit.WinUI;

/// <summary>DPI-aware sizing and branding for WinUI windows.</summary>
public static partial class WindowExtensions
{
   /// <summary>The window's display scale (1.0 at 100%, 1.5 at 150%).</summary>
   public static double DpiScale(this Window window)
   {
      ArgumentNullException.ThrowIfNull(window);
      IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
      uint dpi = GetDpiForWindow(hwnd);
      return dpi == 0 ? 1.0 : dpi / 96.0;
   }



   /// <summary>
   /// Resizes to <paramref name="width"/> x <paramref name="height"/> effective pixels.
   /// AppWindow.Resize takes physical pixels, so this scales by the monitor DPI.
   /// </summary>
   public static void ResizeForDpi(this Window window, int width, int height)
   {
      double scale = window.DpiScale();
      window.AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(width * scale), (int)(height * scale)));
   }



   /// <summary>Keeps the window from shrinking below a usable size, in effective pixels.</summary>
   public static void SetMinimumSize(this Window window, int width, int height)
   {
      double scale = window.DpiScale();
      if(window.AppWindow.Presenter is OverlappedPresenter presenter)
      {
         presenter.PreferredMinimumWidth = (int)(width * scale);
         presenter.PreferredMinimumHeight = (int)(height * scale);
      }
   }



   /// <summary>
   /// Sets the brand icon on the title bar (and taskbar / Alt-Tab for unpackaged runs).
   /// The .ico must be copied to the output directory. A missing file is ignored.
   /// </summary>
   public static void SetAppIcon(this Window window, AppInfo app)
   {
      ArgumentNullException.ThrowIfNull(window);
      ArgumentNullException.ThrowIfNull(app);
      string path = Path.Combine(AppContext.BaseDirectory, app.Brand.IconIcoPath);
      if(File.Exists(path))
      {
         window.AppWindow.SetIcon(path);
      }
   }



   /// <summary>Win32 GetDpiForWindow: the effective DPI of the monitor the window is on.</summary>
   [LibraryImport("user32.dll")]
   private static partial uint GetDpiForWindow(IntPtr hwnd);
}
