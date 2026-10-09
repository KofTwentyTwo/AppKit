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



/// <summary>
/// Checks for and installs app updates. Every member is guarded: failures come back as
/// values, never exceptions, because nothing as optional as an update may crash the app.
/// </summary>
public interface IUpdateService
{
   /// <summary>
   /// True when this build can update itself (it was installed by Velopack). False
   /// under the debugger, for loose builds, and for MSIX packages.
   /// </summary>
   bool IsSupported { get; }

   /// <summary>The installed version, or null outside installed builds.</summary>
   string? CurrentVersion { get; }



   /// <summary>Looks for a newer version and remembers it for <see cref="DownloadAndApplyAsync"/>.</summary>
   Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);



   /// <summary>
   /// Downloads the update found by the last successful <see cref="CheckAsync"/>,
   /// applies it, and restarts the app. On success this never returns (the process
   /// exits to restart); otherwise it returns an error message.
   /// </summary>
   /// <param name="progress">Receives download progress, 0..100.</param>
   /// <param name="cancellationToken">Cancels the download.</param>
   Task<string?> DownloadAndApplyAsync(IProgress<int>? progress = null, CancellationToken cancellationToken = default);
}
