/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

namespace KofTwentyTwo.AppKit.Updates;

/// <summary>
/// Result of an update check. <see cref="AvailableVersion"/> is the newer version
/// found, or null when already up to date; <see cref="Error"/> is non-null when the
/// check itself failed.
/// </summary>
public sealed record UpdateCheckResult(string? AvailableVersion, string? Error)
{
   /// <summary>No newer version exists.</summary>
   public static UpdateCheckResult UpToDate { get; } = new(null, null);

   /// <summary>True when a newer version was found.</summary>
   public bool IsUpdateAvailable => Error is null && AvailableVersion is not null;



   /// <summary>A newer version was found.</summary>
   public static UpdateCheckResult Available(string version) => new(version, null);



   /// <summary>The check failed.</summary>
   public static UpdateCheckResult Failed(string error) => new(null, error);
}
