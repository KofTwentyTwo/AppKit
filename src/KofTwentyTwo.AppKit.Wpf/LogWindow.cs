/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using KofTwentyTwo.AppKit.Logging;


namespace KofTwentyTwo.AppKit.Wpf;

/// <summary>
/// Non-modal activity-log window: stays open beside the main window, tails the current
/// log file live (timer + follow mode), and shows the raw technical text in monospace.
/// Use <see cref="ShowSingle"/> to keep at most one instance per app.
/// </summary>
public sealed class LogWindow : Window
{
   private static LogWindow? s_current;

   private readonly IActivityLog _log;
   private readonly DispatcherTimer _refresh = new() { Interval = TimeSpan.FromSeconds(1) };
   private readonly TextBlock _pathText = new() { VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Opacity = 0.7, Margin = new Thickness(8, 0, 0, 0) };
   private readonly TextBlock _limitNotice = new() { Text = "Older log content omitted", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0), Visibility = Visibility.Collapsed };
   private readonly TextBox _logText = new() { FontFamily = new FontFamily("Cascadia Mono, Consolas"), FontSize = 12, IsReadOnly = true, BorderThickness = new Thickness(0), HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(12) };
   private readonly ToggleButton _follow = new() { Content = "Follow", IsChecked = true, Padding = new Thickness(10, 4, 10, 4) };
   private readonly ToggleButton _errorsOnly = new() { Content = "Errors only", Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(8, 0, 0, 0) };

   private long _lastLength = -1;



   /// <summary>Creates a log window over <paramref name="log"/>.</summary>
   public LogWindow(IActivityLog log, AppInfo app)
   {
      ArgumentNullException.ThrowIfNull(log);
      ArgumentNullException.ThrowIfNull(app);
      _log = log;
      Title = app.DisplayName + " — Activity log";
      AutomationProperties.SetAutomationId(_logText, "LogText");
      AutomationProperties.SetAutomationId(_limitNotice, "LogTruncationNotice");
      _logText.Loaded += (_, _) => _logText.ScrollToEnd();
      Width = 1000;
      Height = 680;
      this.SetAppIcon(app);

      var copy = new Button { Content = "Copy", Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(8, 0, 0, 0) };
      var openFolder = new Button { Content = "Open logs folder", Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(8, 0, 0, 0) };
      _errorsOnly.Click += (_, _) => Reload();
      copy.Click += (_, _) => Clipboard.SetText(_logText.Text);
      openFolder.Click += (_, _) =>
      {
         if(Directory.Exists(_log.LogDirectory))
         {
            Process.Start(new ProcessStartInfo(_log.LogDirectory) { UseShellExecute = true });
         }
      };

      var toolbar = new DockPanel { Margin = new Thickness(12, 12, 12, 4), LastChildFill = true };
      var buttons = new StackPanel { Orientation = Orientation.Horizontal };
      buttons.Children.Add(_follow);
      buttons.Children.Add(_errorsOnly);
      buttons.Children.Add(copy);
      buttons.Children.Add(openFolder);
      DockPanel.SetDock(buttons, Dock.Left);
      toolbar.Children.Add(buttons);
      DockPanel.SetDock(_limitNotice, Dock.Right);
      toolbar.Children.Add(_limitNotice);
      toolbar.Children.Add(_pathText);

      var root = new DockPanel();
      DockPanel.SetDock(toolbar, Dock.Top);
      root.Children.Add(toolbar);
      root.Children.Add(_logText);
      Content = root;

      _refresh.Tick += (_, _) => RefreshIfChanged();
      _refresh.Start();
      Closed += (_, _) => _refresh.Stop();

      Reload();
   }



   /// <summary>Activates the open log window, or opens one.</summary>
   public static LogWindow ShowSingle(IActivityLog log, AppInfo app)
   {
      if(s_current is null)
      {
         s_current = new LogWindow(log, app);
         s_current.Closed += (_, _) => s_current = null;
         s_current.Show();
      }
      s_current.Activate();
      return s_current;
   }



   /// <summary>Closes the window <see cref="ShowSingle"/> opened, if any.</summary>
   public static void CloseCurrent() => s_current?.Close();



   /// <summary>Timer tick: reloads only when Follow is on and the log file grew or rolled over to a new day.</summary>
   private void RefreshIfChanged()
   {
      if(_follow.IsChecked != true)
      {
         return;
      }

      try
      {
         string path = _log.CurrentLogFilePath;
         long length = File.Exists(path) ? new FileInfo(path).Length : 0;
         if(length != _lastLength)
         {
            Reload();
         }
      }
      catch
      {
         // Diagnostics UI must never take the app down over an unreadable file.
      }
   }



   /// <summary>Rereads the log tail, applies the errors-only filter, and scrolls to the newest entry.</summary>
   private void Reload()
   {
      _pathText.Text = _log.CurrentLogFilePath;
      LogSnapshot snapshot = LogTail.Read(_log.CurrentLogFilePath);
      _lastLength = snapshot.Length;
      _limitNotice.Visibility = snapshot.IsTruncated ? Visibility.Visible : Visibility.Collapsed;
      bool errorsOnly = _errorsOnly.IsChecked == true;
      _logText.Text = LogTail.ToDisplayText(errorsOnly ? LogTail.ErrorsOnly(snapshot.Lines) : snapshot.Lines, errorsOnly, snapshot.IsTruncated);
      _logText.UpdateLayout();
      _logText.ScrollToEnd();
   }
}
