/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Reflection;


namespace KofTwentyTwo.AppKit.Updates;

/// <summary>
/// Self-update via Velopack with releases hosted on the app's public GitHub repository.
/// A prerelease build (a '-' in its version) follows the dev channel, whose packages
/// are attached to GitHub prereleases, so prerelease builds must look at prereleases or
/// they could never find an update; stable builds ignore them. Velopack's channel
/// filtering then matches packages to the installed channel either way.
/// </summary>
public sealed class VelopackUpdateService : IUpdateService
{
   private readonly Func<IUpdateBackend> _createBackend;
   private IUpdateBackend? _backend;
   private bool _updateReady;



   /// <summary>
   /// Updates from <paramref name="repositoryUrl"/>'s GitHub Releases, following the
   /// dev channel when the entry assembly is a prerelease build.
   /// </summary>
   public VelopackUpdateService(Uri repositoryUrl)
       : this(repositoryUrl, Assembly.GetEntryAssembly() is { } entry && BuildVersion.IsPrerelease(entry))
   {
   }



   /// <summary>Updates from <paramref name="repositoryUrl"/>'s GitHub Releases.</summary>
   /// <param name="repositoryUrl">The public GitHub repository, e.g. https://github.com/KofTwentyTwo/gclo.</param>
   /// <param name="includePrereleases">Whether GitHub prereleases (the dev channel) are considered.</param>
   public VelopackUpdateService(Uri repositoryUrl, bool includePrereleases)
       : this(() => new VelopackGithubBackend(repositoryUrl, includePrereleases))
   {
      ArgumentNullException.ThrowIfNull(repositoryUrl);
   }



   /// <summary>Test seam. The backend is created lazily so constructing the service can never fail.</summary>
   internal VelopackUpdateService(Func<IUpdateBackend> createBackend)
   {
      _createBackend = createBackend;
   }



   /// <inheritdoc/>
   public bool IsSupported
   {
      get
      {
         try
         {
            return Backend.IsInstalled;
         }
         catch
         {
            return false;
         }
      }
   }



   /// <inheritdoc/>
   public string? CurrentVersion
   {
      get
      {
         try
         {
            return Backend.CurrentVersion;
         }
         catch
         {
            return null;
         }
      }
   }



   private IUpdateBackend Backend => _backend ??= _createBackend();



   /// <inheritdoc/>
   public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
   {
      _updateReady = false;
      try
      {
         if(!Backend.IsInstalled)
         {
            return UpdateCheckResult.Failed(UpdateText.NotSupported);
         }

         string? version = await Backend.CheckAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
         _updateReady = version is not null;
         return version is null ? UpdateCheckResult.UpToDate : UpdateCheckResult.Available(version);
      }
      catch(Exception ex)
      {
         return UpdateCheckResult.Failed(ex.Message);
      }
   }



   /// <inheritdoc/>
   public async Task<string?> DownloadAndApplyAsync(IProgress<int>? progress = null, CancellationToken cancellationToken = default)
   {
      if(!_updateReady || _backend is null)
      {
         return "No update is ready to install; check for updates first.";
      }

      try
      {
         await _backend.DownloadAsync(progress is null ? null : progress.Report, cancellationToken).ConfigureAwait(false);
         _backend.ApplyAndRestart();
         return null;
      }
      catch(Exception ex)
      {
         return ex.Message;
      }
   }
}
