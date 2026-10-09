using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace KofTwentyTwo.AppKit.WinUI;

/// <summary>
/// About dialog built from <see cref="AppInfo"/>: brand header with the build identity,
/// description, repository link, license, and third-party attributions. Set
/// <c>XamlRoot</c> and show it through <see cref="DialogGuard"/>,
/// or use <see cref="ShowAsync(AppInfo, XamlRoot, Assembly?)"/>.
/// </summary>
public sealed partial class AboutDialog : ContentDialog
{
    /// <summary>Builds the dialog for <paramref name="app"/>.</summary>
    /// <param name="app">Whose About this is.</param>
    /// <param name="versionAssembly">Assembly whose build identity is shown; the entry assembly when null.</param>
    public AboutDialog(AppInfo app, Assembly? versionAssembly = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        Title = "About " + app.DisplayName;
        CloseButtonText = "Close";
        DefaultButton = ContentDialogButton.Close;

        var body = new StackPanel { Spacing = 12, Padding = new Thickness(0, 0, 12, 0) };
        body.Children.Add(Header(app, versionAssembly ?? Assembly.GetEntryAssembly()));

        if (app.Description.Length > 0)
        {
            body.Children.Add(new TextBlock { Text = app.Description, TextWrapping = TextWrapping.Wrap });
        }
        if (app.RepositoryUrl is not null)
        {
            body.Children.Add(new HyperlinkButton { Padding = new Thickness(0), Content = app.RepositoryUrl.ToString(), NavigateUri = app.RepositoryUrl });
        }

        var license = Section("License");
        license.Children.Add(new TextBlock { Text = $"{app.DisplayName} is open source, released under the {app.License}.", TextWrapping = TextWrapping.Wrap });
        if (app.Copyright.Length > 0)
        {
            license.Children.Add(new TextBlock { Text = app.Copyright, TextWrapping = TextWrapping.Wrap });
        }
        body.Children.Add(license);

        if (app.Attributions.Count > 0)
        {
            var components = Section("Open source components");
            foreach (Attribution attribution in app.Attributions)
            {
                var line = new TextBlock { TextWrapping = TextWrapping.Wrap, Style = ThemeStyle("CaptionTextBlockStyle") };
                line.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = attribution.Name, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
                line.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = " — " + attribution.License });
                components.Children.Add(line);
            }
            body.Children.Add(components);
        }

        Content = new ScrollViewer { MaxHeight = 460, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = body };
    }

    /// <summary>Shows the About dialog for <paramref name="app"/> (ignored if another dialog is open).</summary>
    public static Task<ContentDialogResult?> ShowAsync(AppInfo app, XamlRoot xamlRoot, Assembly? versionAssembly = null)
        => DialogGuard.ShowAsync(new AboutDialog(app, versionAssembly) { XamlRoot = xamlRoot });

    /// <summary>The splash's look in miniature; deliberately single-theme like the splash.</summary>
    private static Border Header(AppInfo app, Assembly? assembly)
    {
        var title = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        title.Children.Add(new Image { Width = 40, Height = 40, VerticalAlignment = VerticalAlignment.Center, Source = Brand.IconSource(app.Brand) });
        TextBlock name = Brand.Text(app.DisplayName, 28, strong: true);
        name.FontFamily = new FontFamily("Segoe UI Variable Display");
        name.VerticalAlignment = VerticalAlignment.Center;
        title.Children.Add(name);

        var stack = new StackPanel { Padding = new Thickness(20, 16, 20, 16), Spacing = 4 };
        stack.Children.Add(title);
        if (app.Tagline.Length > 0)
        {
            stack.Children.Add(Brand.Text(app.Tagline, 13, 0.8));
        }
        if (assembly is not null)
        {
            TextBlock version = Brand.Text("Version " + BuildVersion.Describe(assembly), 12, 0.7);
            AutomationProperties.SetAutomationId(version, "AboutVersionText");
            stack.Children.Add(version);
        }

        return new Border { CornerRadius = new CornerRadius(8), RequestedTheme = ElementTheme.Dark, Background = Brand.GradientBrush(app.Brand), Child = stack };
    }

    private static StackPanel Section(string heading)
    {
        var section = new StackPanel { Spacing = 4 };
        section.Children.Add(new TextBlock { Text = heading, Style = ThemeStyle("BodyStrongTextBlockStyle") });
        return section;
    }

    private static Style? ThemeStyle(string key)
        => Application.Current.Resources.TryGetValue(key, out object? style) ? style as Style : null;
}
