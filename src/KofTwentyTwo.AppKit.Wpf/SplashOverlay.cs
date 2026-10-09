/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using KofTwentyTwo.AppKit.Settings;


namespace KofTwentyTwo.AppKit.Wpf;

/// <summary>
/// Branded startup splash rendered as a full-window overlay inside the main window,
/// matching the WinUI package: no separate splash window to race with on close, and
/// the gradient and white type are the brand look in both themes.
/// </summary>
public sealed class SplashOverlay : UserControl
{
   private bool _dismissed;



   /// <summary>Builds the overlay for <paramref name="app"/>.</summary>
   /// <param name="app">Whose splash this is.</param>
   /// <param name="versionAssembly">Assembly whose build identity is shown; the entry assembly when null.</param>
   public SplashOverlay(AppInfo app, Assembly? versionAssembly = null)
   {
      ArgumentNullException.ThrowIfNull(app);
      AutomationProperties.SetName(this, $"{app.DisplayName} is starting");
      AutomationProperties.SetAutomationId(this, "AppKitSplash");

      var root = new Grid { Background = Brand.GradientBrush(app.Brand) };

      var center = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
      if(Brand.Icon(app.Brand) is { } icon)
      {
         center.Children.Add(new Image { Width = 96, Height = 96, Margin = new Thickness(0, 0, 0, 16), Source = icon });
      }
      TextBlock title = Brand.Text(app.DisplayName, 40, strong: true);
      title.FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI");
      center.Children.Add(title);
      if(app.Tagline.Length > 0)
      {
         TextBlock tagline = Brand.Text(app.Tagline, 14, 0.8);
         tagline.Margin = new Thickness(0, 4, 0, 0);
         center.Children.Add(tagline);
      }
      Assembly? assembly = versionAssembly ?? Assembly.GetEntryAssembly();
      if(assembly is not null)
      {
         TextBlock version = Brand.Text(BuildVersion.Describe(assembly), 12, 0.6);
         version.Margin = new Thickness(0, 8, 0, 0);
         center.Children.Add(version);
      }
      root.Children.Add(center);

      string contact = Credits.ContactLine(app);
      if(app.Author.Length > 0 || contact.Length > 0)
      {
         var credit = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 44) };
         if(app.Author.Length > 0)
         {
            credit.Children.Add(Brand.Text("Created by " + app.Author, 13, 0.85, strong: true));
         }
         if(contact.Length > 0)
         {
            credit.Children.Add(Brand.Text(contact, 12, 0.6));
         }
         root.Children.Add(credit);
      }

      root.Children.Add(new ProgressBar
      {
         IsIndeterminate = true,
         Height = 3,
         Foreground = Brushes.White,
         Background = new SolidColorBrush(ArgbColor.Parse("#33FFFFFF").ToColor()),
         BorderThickness = new Thickness(0),
         VerticalAlignment = VerticalAlignment.Bottom,
         Margin = new Thickness(0, 0, 0, 20),
      });

      Content = root;
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
      overlay.Dispatcher.InvokeAsync(async () =>
      {
         await Task.Delay(settings.SplashMilliseconds);
         await overlay.DismissAsync();
      });
      return overlay;
   }



   /// <summary>
   /// Fades the overlay out and removes it from its parent panel. Safe to call more
   /// than once; only the first call does anything.
   /// </summary>
   public Task DismissAsync()
   {
      if(_dismissed)
      {
         return Task.CompletedTask;
      }
      _dismissed = true;

      // Input must fall through to the app the moment dismissal starts.
      IsHitTestVisible = false;

      var completion = new TaskCompletionSource();
      var fade = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(250)));
      fade.Completed += (_, _) =>
      {
         (Parent as Panel)?.Children.Remove(this);
         completion.TrySetResult();
      };
      BeginAnimation(OpacityProperty, fade);
      return completion.Task;
   }
}
