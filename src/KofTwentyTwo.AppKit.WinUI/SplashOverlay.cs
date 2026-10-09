/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Reflection;
using KofTwentyTwo.AppKit.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;


namespace KofTwentyTwo.AppKit.WinUI;

/// <summary>
/// Branded startup splash rendered as a full-window overlay inside the main window.
/// An overlay instead of a separate splash window by design: closing a real window
/// while assistive technology or UI automation is querying it crashes the process with
/// a stowed XAML exception, whereas fading out and removing an element is ordinary,
/// race-free XAML work. Deliberately single-theme: the gradient and white type are the
/// brand look in both OS themes.
/// </summary>
public sealed partial class SplashOverlay : UserControl
{
   private readonly Storyboard _fadeOut = new();
   private bool _dismissed;



   /// <summary>Builds the overlay for <paramref name="app"/>.</summary>
   /// <param name="app">Whose splash this is.</param>
   /// <param name="versionAssembly">Assembly whose build identity is shown; the entry assembly when null.</param>
   public SplashOverlay(AppInfo app, Assembly? versionAssembly = null)
   {
      ArgumentNullException.ThrowIfNull(app);

      var root = new Grid { RequestedTheme = ElementTheme.Dark, Background = Brand.GradientBrush(app.Brand) };
      AutomationProperties.SetName(root, $"{app.DisplayName} is starting");
      AutomationProperties.SetAutomationId(root, "AppKitSplash");

      root.Children.Add(BuildCenter(app, versionAssembly ?? Assembly.GetEntryAssembly()));
      if(BuildCredit(app) is { } credit)
      {
         root.Children.Add(credit);
      }
      root.Children.Add(BuildActivityStrip());

      var fade = new DoubleAnimation { To = 0, Duration = new Duration(TimeSpan.FromMilliseconds(250)) };
      Storyboard.SetTarget(fade, root);
      Storyboard.SetTargetProperty(fade, "Opacity");
      _fadeOut.Children.Add(fade);

      Content = root;
   }



   /// <summary>
   /// The centered brand block: the vector mark, the name, the tagline, and the build
   /// identity of <paramref name="assembly"/> when there is one.
   /// </summary>
   private static StackPanel BuildCenter(AppInfo app, Assembly? assembly)
   {
      var center = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
      center.Children.Add(new Image
      {
         Width = 96,
         Height = 96,
         Margin = new Thickness(0, 0, 0, 16),
         HorizontalAlignment = HorizontalAlignment.Center,
         Source = Brand.IconSource(app.Brand),
      });
      TextBlock title = Brand.Text(app.DisplayName, 40, strong: true);
      title.FontFamily = new FontFamily("Segoe UI Variable Display");
      AddCentered(center, title);
      if(app.Tagline.Length > 0)
      {
         TextBlock tagline = Brand.Text(app.Tagline, 14, 0.8);
         tagline.Margin = new Thickness(0, 4, 0, 0);
         AddCentered(center, tagline);
      }
      if(assembly is not null)
      {
         TextBlock version = Brand.Text(BuildVersion.Describe(assembly), 12, 0.6);
         version.Margin = new Thickness(0, 8, 0, 0);
         AddCentered(center, version);
      }
      return center;
   }



   /// <summary>
   /// The author credit anchored above the activity strip, or null when the app declares
   /// neither an author nor any contact details.
   /// </summary>
   private static StackPanel? BuildCredit(AppInfo app)
   {
      string contact = Credits.ContactLine(app);
      if(app.Author.Length == 0 && contact.Length == 0)
      {
         return null;
      }

      var credit = new StackPanel
      {
         HorizontalAlignment = HorizontalAlignment.Center,
         VerticalAlignment = VerticalAlignment.Bottom,
         Margin = new Thickness(0, 0, 0, 44),
         Spacing = 3,
      };
      if(app.Author.Length > 0)
      {
         AddCentered(credit, Brand.Text("Created by " + app.Author, 13, 0.85, strong: true));
      }
      if(contact.Length > 0)
      {
         AddCentered(credit, Brand.Text(contact, 12, 0.6));
      }
      return credit;
   }



   /// <summary>
   /// The thin activity strip at the bottom: a white bar over a 20% white track, with the
   /// track height resource overridden so the track matches the 3px bar.
   /// </summary>
   private static ProgressBar BuildActivityStrip()
   {
      var progress = new ProgressBar
      {
         IsIndeterminate = true,
         Height = 3,
         MinHeight = 3,
         Foreground = Brand.White,
         Background = new SolidColorBrush(ArgbColor.Parse("#33FFFFFF").ToColor()),
         VerticalAlignment = VerticalAlignment.Bottom,
         Margin = new Thickness(0, 0, 0, 20),
      };
      progress.Resources["ProgressBarTrackHeight"] = 3.0;
      return progress;
   }



   /// <summary>
   /// Shows a splash over <paramref name="host"/> (the window's root Grid) when
   /// <paramref name="settings"/> enables it, and dismisses it after the configured
   /// time. Returns the overlay, or null when the splash is turned off.
   /// </summary>
   public static SplashOverlay? ShowOver(Panel host, AppInfo app, ShellSettings settings, Assembly? versionAssembly = null)
   {
      ArgumentNullException.ThrowIfNull(host);
      ArgumentNullException.ThrowIfNull(settings);
      if(!settings.ShowSplashScreen)
      {
         return null;
      }

      var overlay = new SplashOverlay(app, versionAssembly);
      if(host is Grid grid)
      {
         Grid.SetRowSpan(overlay, Math.Max(grid.RowDefinitions.Count, 1));
         Grid.SetColumnSpan(overlay, Math.Max(grid.ColumnDefinitions.Count, 1));
      }
      host.Children.Add(overlay);
      ///////////////////////////////////////////////////////////////////////////////
      // start the countdown through the dispatcher queue, not directly: the queue //
      // runs it on the UI thread with its synchronization context in place, so    //
      // the fade after the delay also runs on the UI thread                       //
      ///////////////////////////////////////////////////////////////////////////////
      overlay.DispatcherQueue.TryEnqueue(() => _ = overlay.DismissAfterAsync(settings.SplashMilliseconds));
      return overlay;
   }



   /// <summary>Waits <paramref name="milliseconds"/>, then fades the overlay out.</summary>
   private async Task DismissAfterAsync(int milliseconds)
   {
      await Task.Delay(milliseconds);
      await DismissAsync();
   }



   /// <summary>
   /// Fades the overlay out and removes it from its parent panel. Safe to call more
   /// than once; only the first call does anything.
   /// </summary>
   public async Task DismissAsync()
   {
      if(_dismissed)
      {
         return;
      }
      _dismissed = true;

      // Input must fall through to the app the moment dismissal starts.
      IsHitTestVisible = false;

      var completion = new TaskCompletionSource();
      _fadeOut.Completed += (_, _) => completion.TrySetResult();
      _fadeOut.Begin();
      await completion.Task;

      (Parent as Panel)?.Children.Remove(this);
   }



   /// <summary>Centers an element horizontally and adds it to the panel.</summary>
   private static void AddCentered(Panel panel, FrameworkElement element)
   {
      element.HorizontalAlignment = HorizontalAlignment.Center;
      panel.Children.Add(element);
   }
}
