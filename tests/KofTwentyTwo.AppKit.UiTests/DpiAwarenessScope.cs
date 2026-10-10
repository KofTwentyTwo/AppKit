/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.ComponentModel;
using System.Runtime.InteropServices;


namespace KofTwentyTwo.AppKit.UiTests;

/// <summary>Uses physical screen coordinates for UI input and capture, restoring the synchronous test thread afterwards.</summary>
internal sealed class DpiAwarenessScope : IDisposable
{
   private readonly nint _previous;



   /// <summary>Disables DPI virtualization on this thread with per-monitor awareness v2 (Windows 10 1703+).</summary>
   public DpiAwarenessScope()
   {
      _previous = SetThreadDpiAwarenessContext(-4);
      if(_previous == 0)
      {
         throw new Win32Exception(Marshal.GetLastPInvokeError());
      }
   }



   /// <summary>Restores the context on the same thread that ran the synchronous UI test.</summary>
   public void Dispose() => SetThreadDpiAwarenessContext(_previous);



   /// <summary>Windows' thread-scoped DPI override, supported by the repository's minimum OS.</summary>
   [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
   private static extern nint SetThreadDpiAwarenessContext(nint dpiContext);
}
