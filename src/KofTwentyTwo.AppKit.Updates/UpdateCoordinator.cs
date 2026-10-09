/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Interaction;
using KofTwentyTwo.AppKit.Logging;


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
