using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KofTwentyTwo.AppKit.Wpf;

/// <summary>Turns an <see cref="AppBrand"/> into WPF brushes and images.</summary>
public static class Brand
{
    /// <summary>The WPF color for <paramref name="color"/>.</summary>
    public static Color ToColor(this ArgbColor color) => Color.FromArgb(color.A, color.R, color.G, color.B);

    /// <summary>The brand's diagonal gradient.</summary>
    public static LinearGradientBrush GradientBrush(AppBrand brand)
    {
        ArgumentNullException.ThrowIfNull(brand);
        var brush = new LinearGradientBrush(ArgbColor.Parse(brand.GradientStart).ToColor(), ArgbColor.Parse(brand.GradientEnd).ToColor(), new Point(0, 0), new Point(1, 1));
        brush.Freeze();
        return brush;
    }

    /// <summary>
    /// The brand mark from the app's .ico (WPF has no native SVG support): the largest
    /// frame, so it stays crisp when scaled. Null when the file is missing or unreadable.
    /// </summary>
    public static BitmapSource? Icon(AppBrand brand)
    {
        ArgumentNullException.ThrowIfNull(brand);
        string path = Path.Combine(AppContext.BaseDirectory, brand.IconIcoPath);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var decoder = BitmapDecoder.Create(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            return decoder.Frames.OrderByDescending(frame => frame.PixelWidth).First();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>White text at the given opacity over the brand gradient.</summary>
    internal static TextBlock Text(string text, double size, double opacity = 1, bool strong = false)
        => new()
        {
            Text = text,
            FontSize = size,
            Foreground = Brushes.White,
            Opacity = opacity,
            FontWeight = strong ? FontWeights.SemiBold : FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
}
