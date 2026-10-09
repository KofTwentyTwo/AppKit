namespace KofTwentyTwo.AppKit.Interaction;

/// <summary>
/// The minimal dialog surface shared flows (such as the update check) need. Each UI
/// package implements it with its framework's dialogs, so the flow logic itself stays
/// framework-free and unit-tested.
/// </summary>
public interface IUserPrompter
{
   /// <summary>Shows an informational message with a single dismiss button.</summary>
   Task ShowMessageAsync(string title, string message);



   /// <summary>
   /// Asks a yes/no question. True only when the user chose
   /// <paramref name="confirmText"/>; dismissing or a suppressed dialog is false.
   /// </summary>
   Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText);
}
