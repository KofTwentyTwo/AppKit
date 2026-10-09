using KofTwentyTwo.AppKit.Interaction;
using Microsoft.UI.Xaml;
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



/// <summary>
/// <see cref="IUserPrompter"/> over guarded ContentDialogs, for shared flows such as
/// <see cref="Updates.UpdateCoordinator"/>.
/// </summary>
/// <param name="xamlRoot">Supplies the XamlRoot to host dialogs, usually <c>() =&gt; window.Content.XamlRoot</c>.</param>
public sealed class ContentDialogPrompter(Func<XamlRoot> xamlRoot) : IUserPrompter
{
   /// <inheritdoc/>
   public async Task ShowMessageAsync(string title, string message)
   {
      await DialogGuard.ShowAsync(new ContentDialog
      {
         Title = title,
         Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
         CloseButtonText = "OK",
         DefaultButton = ContentDialogButton.Close,
         XamlRoot = xamlRoot(),
      });
   }



   /// <inheritdoc/>
   public async Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText)
   {
      ContentDialogResult? result = await DialogGuard.ShowAsync(new ContentDialog
      {
         Title = title,
         Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
         PrimaryButtonText = confirmText,
         CloseButtonText = cancelText,
         DefaultButton = ContentDialogButton.Primary,
         XamlRoot = xamlRoot(),
      });
      return result == ContentDialogResult.Primary;
   }
}
