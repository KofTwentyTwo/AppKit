using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;


namespace KofTwentyTwo.AppKit.Wpf;

/// <summary>
/// About window built from <see cref="AppInfo"/>: brand header with the build identity,
/// description, repository link, license, and third-party attributions. Same content as
/// the WinUI AboutDialog, as a modal window.
/// </summary>
public sealed class AboutWindow : Window
{
   /// <summary>Builds the window for <paramref name="app"/>.</summary>
   /// <param name="app">Whose About this is.</param>
   /// <param name="versionAssembly">Assembly whose build identity is shown; the entry assembly when null.</param>
   public AboutWindow(AppInfo app, Assembly? versionAssembly = null)
   {
      ArgumentNullException.ThrowIfNull(app);
      Title = "About " + app.DisplayName;
      Width = 480;
      SizeToContent = SizeToContent.Height;
      MaxHeight = 640;
      ResizeMode = ResizeMode.NoResize;
      WindowStartupLocation = WindowStartupLocation.CenterOwner;
      ShowInTaskbar = false;
      this.SetAppIcon(app);

      var body = new StackPanel { Margin = new Thickness(20) };
      body.Children.Add(Header(app, versionAssembly ?? Assembly.GetEntryAssembly()));

      if(app.Description.Length > 0)
      {
         body.Children.Add(Paragraph(app.Description));
      }
      if(app.RepositoryUrl is not null)
      {
         var link = new Hyperlink(new Run(app.RepositoryUrl.ToString())) { NavigateUri = app.RepositoryUrl };
         link.RequestNavigate += (_, e) => Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
         body.Children.Add(new TextBlock(link) { Margin = new Thickness(0, 12, 0, 0) });
      }

      body.Children.Add(Heading("License"));
      body.Children.Add(Paragraph($"{app.DisplayName} is open source, released under the {app.License}.", top: 4));
      if(app.Copyright.Length > 0)
      {
         body.Children.Add(Paragraph(app.Copyright, top: 4));
      }

      if(app.Attributions.Count > 0)
      {
         body.Children.Add(Heading("Open source components"));
         foreach(Attribution attribution in app.Attributions)
         {
            var line = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12, Margin = new Thickness(0, 4, 0, 0) };
            line.Inlines.Add(new Run(attribution.Name) { FontWeight = FontWeights.SemiBold });
            line.Inlines.Add(new Run(" — " + attribution.License));
            body.Children.Add(line);
         }
      }

      var close = new Button { Content = "Close", IsDefault = true, IsCancel = true, MinWidth = 88, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
      body.Children.Add(close);

      Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = body };
   }



   /// <summary>Shows the About window modally over <paramref name="owner"/>.</summary>
   public static void Show(AppInfo app, Window? owner, Assembly? versionAssembly = null)
       => _ = new AboutWindow(app, versionAssembly) { Owner = owner }.ShowDialog();



   private static Border Header(AppInfo app, Assembly? assembly)
   {
      var title = new StackPanel { Orientation = Orientation.Horizontal };
      if(Brand.Icon(app.Brand) is { } icon)
      {
         title.Children.Add(new Image { Width = 40, Height = 40, Margin = new Thickness(0, 0, 12, 0), Source = icon });
      }
      TextBlock name = Brand.Text(app.DisplayName, 28, strong: true);
      name.FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI");
      name.VerticalAlignment = VerticalAlignment.Center;
      title.Children.Add(name);

      var stack = new StackPanel { Margin = new Thickness(20, 16, 20, 16) };
      stack.Children.Add(title);
      if(app.Tagline.Length > 0)
      {
         TextBlock tagline = Brand.Text(app.Tagline, 13, 0.8);
         tagline.HorizontalAlignment = HorizontalAlignment.Left;
         stack.Children.Add(tagline);
      }
      if(assembly is not null)
      {
         TextBlock version = Brand.Text("Version " + BuildVersion.Describe(assembly), 12, 0.7);
         version.HorizontalAlignment = HorizontalAlignment.Left;
         AutomationProperties.SetAutomationId(version, "AboutVersionText");
         stack.Children.Add(version);
      }

      return new Border { CornerRadius = new CornerRadius(8), Background = Brand.GradientBrush(app.Brand), Child = stack };
   }



   private static TextBlock Heading(string text)
       => new() { Text = text, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 16, 0, 0) };



   private static TextBlock Paragraph(string text, double top = 12)
       => new() { Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, top, 0, 0) };
}
