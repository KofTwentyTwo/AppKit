/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;


namespace KofTwentyTwo.AppKit.WinUI;

/// <summary>
/// Non-modal activity-log window: stays open beside the main window, tails the current
/// log file live (timer + follow mode), and shows the raw technical text (timestamps,
/// levels, full exception stacks) in monospace. Use <see cref="ShowSingle"/> to keep at
/// most one instance per app.
/// </summary>
public sealed partial class LogWindow : Window
{
   private static readonly TimeSpan s_refreshInterval = TimeSpan.FromSeconds(1);
   private static LogWindow? s_current;

   private readonly IActivityLog _log;
   private readonly DispatcherTimer _refresh = new() { Interval = s_refreshInterval };
   private readonly TextBlock _pathText = new() { VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Opacity = 0.7 };
   private readonly TextBlock _logText = new() { FontFamily = new FontFamily("Cascadia Mono, Consolas"), FontSize = 12, IsTextSelectionEnabled = true, TextWrapping = TextWrapping.NoWrap };
   private readonly ScrollViewer _scroll = new() { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(12) };
   private readonly ToggleButton _follow = new() { Content = "Follow", IsChecked = true };
   private readonly ToggleButton _errorsOnly = new() { Content = "Errors only" };

   private long _lastLength = -1;



   /// <summary>Creates a log window over <paramref name="log"/>.</summary>
   public LogWindow(IActivityLog log, AppInfo app)
   {
      ArgumentNullException.ThrowIfNull(log);
      ArgumentNullException.ThrowIfNull(app);
      _log = log;
      Title = app.DisplayName + " — Activity log";

      var copy = new Button { Content = "Copy" };
      var openFolder = new Button { Content = "Open logs folder" };
      _errorsOnly.Click += (_, _) => Reload();
      copy.Click += (_, _) => CopyToClipboard();
      openFolder.Click += async (_, _) =>
      {
         if(Directory.Exists(_log.LogDirectory))
         {
            await Windows.System.Launcher.LaunchFolderPathAsync(_log.LogDirectory);
         }
      };

      var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Padding = new Thickness(12, 12, 12, 4) };
      toolbar.Children.Add(_follow);
      toolbar.Children.Add(_errorsOnly);
      toolbar.Children.Add(copy);
      toolbar.Children.Add(openFolder);
      toolbar.Children.Add(_pathText);
      _scroll.Content = _logText;

      var root = new Grid();
      root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
      root.Children.Add(toolbar);
      Grid.SetRow(_scroll, 1);
      root.Children.Add(_scroll);
      Content = root;

      this.ResizeForDpi(1000, 680);
      this.SetAppIcon(app);

      _refresh.Tick += (_, _) => RefreshIfChanged();
      _refresh.Start();
      Closed += (_, _) => _refresh.Stop();

      Reload();
   }



   /// <summary>
   /// Activates the open log window, or opens one. Close it from the main window's
   /// Closed handler (<see cref="CloseCurrent"/>) or a log-only process lingers.
   /// </summary>
   public static LogWindow ShowSingle(IActivityLog log, AppInfo app)
   {
      if(s_current is null)
      {
         s_current = new LogWindow(log, app);
         s_current.Closed += (_, _) => s_current = null;
      }
      s_current.Activate();
      return s_current;
   }



   /// <summary>Closes the window <see cref="ShowSingle"/> opened, if any.</summary>
   public static void CloseCurrent() => s_current?.Close();



   /// <summary>Copies the visible log text to the clipboard.</summary>
   private void CopyToClipboard()
   {
      var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
      package.SetText(_logText.Text);
      Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
   }



   /// <summary>Timer tick: reload only when Follow is on and the file grew or rolled over.</summary>
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
      _pathText.Text = _log.CurrentLogFilePath; // the day can roll over while the window is open
      LogSnapshot snapshot = LogTail.Read(_log.CurrentLogFilePath);
      _lastLength = snapshot.Length;
      bool errorsOnly = _errorsOnly.IsChecked == true;
      _logText.Text = LogTail.ToDisplayText(errorsOnly ? LogTail.ErrorsOnly(snapshot.Lines) : snapshot.Lines, errorsOnly, snapshot.IsTruncated);

      _scroll.UpdateLayout();
      _scroll.ChangeView(null, _scroll.ScrollableHeight, null, disableAnimation: true);
   }
}
