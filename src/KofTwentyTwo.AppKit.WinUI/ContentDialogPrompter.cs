/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Interaction;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace KofTwentyTwo.AppKit.WinUI;

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
