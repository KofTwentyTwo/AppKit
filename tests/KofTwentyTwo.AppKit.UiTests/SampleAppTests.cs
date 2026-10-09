/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using FlaUI.Core.AutomationElements;


namespace KofTwentyTwo.AppKit.UiTests;

/// <summary>
/// End-to-end smoke tests: every shell piece AppKit provides, driven through the real
/// WinUI and WPF sample apps. UI tests share one desktop, so they never run in parallel.
/// </summary>
[Collection("Desktop")]
public class SampleAppTests
{
   /// <summary>Splash: shows then dismisses itself.</summary>
   [Theory]
   [InlineData(SampleKind.WinUI)]
   [InlineData(SampleKind.Wpf)]
   public void Splash_ShowsThenDismissesItself(SampleKind kind)
   {
      using var session = new AppSession(kind, showSplash: true);
      session.WaitForElement("AppKitSplash");
      session.WaitForElementGone("AppKitSplash", TimeSpan.FromSeconds(15));
   }



   /// <summary>About: shows the build version.</summary>
   [Theory]
   [InlineData(SampleKind.WinUI)]
   [InlineData(SampleKind.Wpf)]
   public void About_ShowsTheBuildVersion(SampleKind kind)
   {
      using var session = new AppSession(kind);
      session.InvokeMenuItem("Help", "AboutMenuItem");
      AutomationElement version = session.WaitForElement("AboutVersionText");
      Assert.StartsWith("Version 0.", version.Name, StringComparison.Ordinal);
      session.ClickButton("Close");
      session.WaitForElementGone("AboutVersionText");
   }



   /// <summary>UnhandledException: is logged and the app survives.</summary>
   [Theory]
   [InlineData(SampleKind.WinUI)]
   [InlineData(SampleKind.Wpf)]
   public void UnhandledException_IsLoggedAndTheAppSurvives(SampleKind kind)
   {
      using var session = new AppSession(kind);
      session.MouseClickElement("ThrowButton");
      session.WaitFor(
          () => session.ReadLogs().Contains("Deliberately unhandled sample exception", StringComparison.Ordinal) ? session.MainWindow : null,
          "the crash net's log entry");
      Assert.False(session.App.HasExited);
      Assert.Contains("Unhandled UI exception", session.ReadLogs(), StringComparison.Ordinal);
   }



   /// <summary>ActivityLog: opens a window showing the log.</summary>
   [Theory]
   [InlineData(SampleKind.WinUI)]
   [InlineData(SampleKind.Wpf)]
   public void ActivityLog_OpensAWindowShowingTheLog(SampleKind kind)
   {
      using var session = new AppSession(kind);
      session.InvokeMenuItem("Help", "ActivityLogMenuItem");
      session.WaitFor(
          () => session.TopLevelWindows().FirstOrDefault(w => string.Equals(w.Name, "AppKit Sample — Activity log", StringComparison.Ordinal)),
          "the activity log window");
   }



   /// <summary>ThemeChoice: is saved.</summary>
   [Theory]
   [InlineData(SampleKind.WinUI)]
   [InlineData(SampleKind.Wpf)]
   public void ThemeChoice_IsSaved(SampleKind kind)
   {
      using var session = new AppSession(kind);
      session.InvokeMenuItem("View", "ThemeDark");
      session.WaitFor(
          () => session.ReadSettings().Contains("\"Theme\": \"Dark\"", StringComparison.Ordinal) ? session.MainWindow : null,
          "settings.json to record the dark theme");
   }



   /// <summary>CheckForUpdates: in a loose build says updates need an install.</summary>
   [Theory]
   [InlineData(SampleKind.WinUI)]
   [InlineData(SampleKind.Wpf)]
   public void CheckForUpdates_InALooseBuild_SaysUpdatesNeedAnInstall(SampleKind kind)
   {
      using var session = new AppSession(kind);
      session.InvokeMenuItem("Help", "CheckForUpdatesMenuItem");
      session.WaitFor(
          () => session.FindInApp(cf => cf.ByName("Updates are only available in installed builds.")),
          "the not-installed message");
      session.ClickButton("OK");
   }
}



/// <summary>UI tests share one desktop, so the collection never runs in parallel.</summary>
[CollectionDefinition("Desktop", DisableParallelization = true)]
public class DesktopCollectionDefinition
{
}
