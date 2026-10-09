/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Interaction;
using KofTwentyTwo.AppKit.Logging;


namespace KofTwentyTwo.AppKit.Updates;

/// <summary>
/// The "check for updates" flows, written once against <see cref="IUpdateService"/> and
/// <see cref="IUserPrompter"/> so WinUI and WPF apps behave identically and the logic is
/// unit-tested.
/// </summary>
public sealed class UpdateCoordinator
{
   private readonly IUpdateService _updates;
   private readonly IUserPrompter _prompter;
   private readonly string _appName;
   private readonly IActivityLog _log;



   /// <summary>Creates the coordinator.</summary>
   /// <param name="updates">The update service.</param>
   /// <param name="prompter">The UI's dialog adapter.</param>
   /// <param name="appName">Display name used in prompts.</param>
   /// <param name="log">Activity log; discarded when null.</param>
   public UpdateCoordinator(IUpdateService updates, IUserPrompter prompter, string appName, IActivityLog? log = null)
   {
      ArgumentNullException.ThrowIfNull(updates);
      ArgumentNullException.ThrowIfNull(prompter);
      ArgumentException.ThrowIfNullOrWhiteSpace(appName);
      _updates = updates;
      _prompter = prompter;
      _appName = appName;
      _log = log ?? NullActivityLog.Instance;
   }



   /// <summary>
   /// The Help → Check for updates command: reports every outcome to the user, and
   /// offers to install when a newer version exists.
   /// </summary>
   public async Task CheckInteractivelyAsync(CancellationToken cancellationToken = default)
   {
      if(!_updates.IsSupported)
      {
         await _prompter.ShowMessageAsync(UpdateText.CheckTitle, UpdateText.NotSupported).ConfigureAwait(true);
         return;
      }

      UpdateCheckResult result = await _updates.CheckAsync(cancellationToken).ConfigureAwait(true);
      if(result.Error is not null)
      {
         _log.Warning($"Update check failed: {result.Error}");
         await _prompter.ShowMessageAsync(UpdateText.CheckTitle, UpdateText.CheckFailed(result.Error)).ConfigureAwait(true);
         return;
      }

      if(result.AvailableVersion is null)
      {
         await _prompter.ShowMessageAsync(UpdateText.CheckTitle, UpdateText.UpToDate(_updates.CurrentVersion)).ConfigureAwait(true);
         return;
      }

      await OfferAsync(result.AvailableVersion, cancellationToken).ConfigureAwait(true);
   }



   /// <summary>
   /// The startup check: silent unless a newer version exists, in which case the user
   /// is offered the update. Unsupported builds and failures are only logged.
   /// </summary>
   public async Task CheckQuietlyAsync(CancellationToken cancellationToken = default)
   {
      if(!_updates.IsSupported)
      {
         return;
      }

      UpdateCheckResult result = await _updates.CheckAsync(cancellationToken).ConfigureAwait(true);
      if(result.Error is not null)
      {
         _log.Warning($"Startup update check failed: {result.Error}");
         return;
      }

      if(result.AvailableVersion is not null)
      {
         _log.Info($"Update available: v{result.AvailableVersion}.");
         await OfferAsync(result.AvailableVersion, cancellationToken).ConfigureAwait(true);
      }
   }



   /// <summary>Offers the update and, when accepted, downloads it, applies it, and restarts. A failed install is logged and shown, because reaching the end means the restart did not happen.</summary>
   private async Task OfferAsync(string version, CancellationToken cancellationToken)
   {
      bool accepted = await _prompter.ConfirmAsync(
          UpdateText.AvailableTitle, UpdateText.Available(_appName, version), UpdateText.Confirm, UpdateText.Decline).ConfigureAwait(true);
      if(!accepted)
      {
         return;
      }

      _log.Info($"Installing update v{version}.");
      // On success this exits the process to restart into the new version, so
      // reaching the lines below means the update did not go through.
      string? error = await _updates.DownloadAndApplyAsync(null, cancellationToken).ConfigureAwait(true);
      if(error is not null)
      {
         _log.Error($"Update to v{version} failed: {error}");
         await _prompter.ShowMessageAsync(UpdateText.FailedTitle, error).ConfigureAwait(true);
      }
   }
}
