/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Diagnostics;
using System.Windows;


namespace AppKit.Sample.Wpf;

/// <summary>Opens the sample's fixed public Help destinations using the Windows default browser.</summary>
public partial class MainWindow
{
   /// <summary>Shows the sample's source repository.</summary>
   private void GitHub_Click(object sender, RoutedEventArgs args) => OpenHelpPage(SampleApp.Info.RepositoryUrl!);



   /// <summary>Offers the repository's bug/feature templates.</summary>
   private void SubmitIssue_Click(object sender, RoutedEventArgs args) => OpenHelpPage(SampleApp.IssuesUrl);



   /// <summary>Opens the sample's community support destination.</summary>
   private void CommunitySupport_Click(object sender, RoutedEventArgs args) => OpenHelpPage(SampleApp.SupportUrl);



   /// <summary>Surfaces OS/browser launch failure without passing private app data to the shell.</summary>
   private void OpenHelpPage(Uri target)
   {
      try
      {
         using var process = Process.Start(new ProcessStartInfo(target.AbsoluteUri) { UseShellExecute = true });
      }
      catch(Exception exception) when(exception is System.ComponentModel.Win32Exception or InvalidOperationException)
      {
         StatusText.Text = "The link could not be opened. Check your default browser.";
         Program.Log.Warning(FormattableString.Invariant($"A Help link could not be opened ({exception.GetType().Name}, 0x{exception.HResult:X8})."));
      }
   }
}
