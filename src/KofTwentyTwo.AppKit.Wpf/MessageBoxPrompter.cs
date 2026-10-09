/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Windows;
using KofTwentyTwo.AppKit.Interaction;


namespace KofTwentyTwo.AppKit.Wpf;

/// <summary>
/// <see cref="IUserPrompter"/> over WPF message boxes, for shared flows such as
/// <see cref="Updates.UpdateCoordinator"/>. Message boxes cannot relabel their buttons,
/// so confirmations show Yes/No with the confirm text folded into the question.
/// </summary>
/// <param name="owner">Supplies the owning window, usually the main window.</param>
public sealed class MessageBoxPrompter(Func<Window?> owner) : IUserPrompter
{
   /// <inheritdoc/>
   public Task ShowMessageAsync(string title, string message)
   {
      Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
      return Task.CompletedTask;
   }



   /// <inheritdoc/>
   public Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText)
   {
      MessageBoxResult result = Show($"{message}\n\n{confirmText}?", title, MessageBoxButton.YesNo, MessageBoxImage.Question);
      return Task.FromResult(result == MessageBoxResult.Yes);
   }



   /// <summary>Shows a message box owned by the app&apos;s window when there is one, so it stays in front of the app.</summary>
   private MessageBoxResult Show(string message, string title, MessageBoxButton buttons, MessageBoxImage image)
       => owner() is Window window
           ? MessageBox.Show(window, message, title, buttons, image)
           : MessageBox.Show(message, title, buttons, image);
}
