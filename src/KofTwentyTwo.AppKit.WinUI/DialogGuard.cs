/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml.Controls;


namespace KofTwentyTwo.AppKit.WinUI;

/// <summary>
/// Serializes ContentDialog display. WinUI allows a single open ContentDialog per
/// XamlRoot, and a second ShowAsync does not queue: it throws a COMException which,
/// escaping an async void event handler, kills the process with a stowed exception
/// (0xc000027b). Open every dialog through <see cref="ShowAsync"/>, which ignores the
/// request while another dialog is on screen (the same no-op a user expects from
/// commands behind a modal). All calls run on the single UI thread, so the flag needs
/// no locking.
/// </summary>
public static class DialogGuard
{
   private static bool s_dialogOpen;

   /// <summary>True while a guarded dialog is on screen.</summary>
   public static bool IsDialogOpen => s_dialogOpen;



   /// <summary>
   /// Shows <paramref name="dialog"/> unless another dialog is on screen. Returns the
   /// dialog result, or null when the request was ignored; callers treat null like a
   /// dismissal.
   /// </summary>
   public static async Task<ContentDialogResult?> ShowAsync(ContentDialog dialog)
   {
      ArgumentNullException.ThrowIfNull(dialog);
      if(s_dialogOpen)
      {
         return null;
      }

      s_dialogOpen = true;
      try
      {
         return await dialog.ShowAsync();
      }
      finally
      {
         s_dialogOpen = false;
      }
   }
}
