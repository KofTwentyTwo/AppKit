/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

namespace KofTwentyTwo.AppKit.Updates;

/// <summary>
/// The narrow slice of Velopack the update service uses, so the guarding and state
/// logic is testable without an installed app or the network.
/// </summary>
internal interface IUpdateBackend
{
   bool IsInstalled { get; }

   string? CurrentVersion { get; }



   /// <summary>The newer version found (and remembered for download), or null.</summary>
   Task<string?> CheckAsync();



   /// <summary>Downloads the remembered update.</summary>
   Task DownloadAsync(Action<int>? progress, CancellationToken cancellationToken);



   /// <summary>Applies the downloaded update and restarts; exits the process on success.</summary>
   void ApplyAndRestart();
}
