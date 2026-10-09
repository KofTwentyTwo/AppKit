/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Diagnostics.CodeAnalysis;
using Velopack;
using Velopack.Sources;


namespace KofTwentyTwo.AppKit.Updates;

/// <summary>
/// The real <see cref="IUpdateBackend"/>: a thin pass-through to Velopack's
/// UpdateManager over a GitHub Releases source. Excluded from coverage because every
/// member needs an installed app or the network; all decisions around it live in
/// <see cref="VelopackUpdateService"/>, which is fully tested.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Pass-through to Velopack; requires an installed app and network access.")]
internal sealed class VelopackGithubBackend : IUpdateBackend
{
   private readonly UpdateManager _manager;
   private UpdateInfo? _pending;



   /// <summary>Creates a Velopack UpdateManager over the repository&apos;s GitHub Releases, looking at prereleases only when asked.</summary>
   public VelopackGithubBackend(Uri repositoryUrl, bool includePrereleases)
   {
      _manager = new UpdateManager(new GithubSource(repositoryUrl.ToString(), accessToken: null, prerelease: includePrereleases));
   }



   public bool IsInstalled => _manager.IsInstalled;

   public string? CurrentVersion => _manager.CurrentVersion?.ToString();



   /// <summary>Asks Velopack for a newer release and remembers it for the download.</summary>
   public async Task<string?> CheckAsync()
   {
      _pending = await _manager.CheckForUpdatesAsync().ConfigureAwait(false);
      return _pending?.TargetFullRelease.Version.ToString();
   }



   /// <summary>Downloads the remembered update with progress; fails when no update was found.</summary>
   public Task DownloadAsync(Action<int>? progress, CancellationToken cancellationToken)
       => _pending is null
           ? Task.FromException(new InvalidOperationException("No update was found to download."))
           : _manager.DownloadUpdatesAsync(_pending, progress, cancellationToken);



   /// <summary>Applies the downloaded update and restarts the app; Velopack exits the process on success.</summary>
   public void ApplyAndRestart()
   {
      if(_pending is null)
      {
         throw new InvalidOperationException("No update was found to apply.");
      }
      _manager.ApplyUpdatesAndRestart(_pending.TargetFullRelease);
   }
}
