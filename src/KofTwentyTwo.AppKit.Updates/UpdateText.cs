/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

namespace KofTwentyTwo.AppKit.Updates;

/// <summary>User-facing text of the update flow, shared by every UI package.</summary>
public static class UpdateText
{
   /// <summary>Title of the check dialogs.</summary>
   public const string CheckTitle = "Check for updates";

   /// <summary>Title of the confirmation dialog.</summary>
   public const string AvailableTitle = "Update available";

   /// <summary>Title shown when installing failed.</summary>
   public const string FailedTitle = "Update failed";

   /// <summary>Shown in builds that cannot self-update.</summary>
   public const string NotSupported = "Updates are only available in installed builds.";

   /// <summary>Confirm button.</summary>
   public const string Confirm = "Update and restart";

   /// <summary>Decline button.</summary>
   public const string Decline = "Not now";



   /// <summary>"You are up to date (v1.2.3)."</summary>
   public static string UpToDate(string? currentVersion)
       => currentVersion is null ? "You are up to date." : $"You are up to date (v{currentVersion}).";



   /// <summary>Explains a failed check.</summary>
   public static string CheckFailed(string error) => $"Could not check for updates.\n{error}";



   /// <summary>Offers the update.</summary>
   public static string Available(string appName, string version)
       => $"{appName} v{version} is available. The app will restart to finish installing the update.";
}
