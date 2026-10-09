using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;


namespace KofTwentyTwo.AppKit.WinUI;

/// <summary>Turns an <see cref="AppBrand"/> into WinUI brushes and images.</summary>
public static class Brand
{
   /// <summary>The WinUI color for <paramref name="color"/>.</summary>
   public static Windows.UI.Color ToColor(this ArgbColor color)
       => ColorHelper.FromArgb(color.A, color.R, color.G, color.B);



   /// <summary>The brand's diagonal gradient.</summary>
   public static LinearGradientBrush GradientBrush(AppBrand brand)
   {
      ArgumentNullException.ThrowIfNull(brand);
      var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
      brush.GradientStops.Add(new GradientStop { Offset = 0, Color = ArgbColor.Parse(brand.GradientStart).ToColor() });
      brush.GradientStops.Add(new GradientStop { Offset = 1, Color = ArgbColor.Parse(brand.GradientEnd).ToColor() });
      return brush;
   }



   /// <summary>
   /// The vector mark. ms-appx:/// resolves to the app directory both packaged and
   /// unpackaged, so the SVG must be copied to the output (CopyToOutputDirectory).
   /// </summary>
   public static SvgImageSource IconSource(AppBrand brand)
   {
      ArgumentNullException.ThrowIfNull(brand);
      return new SvgImageSource(new Uri("ms-appx:///" + brand.IconSvgPath.TrimStart('/')));
   }



   /// <summary>White text, the brand surfaces' only foreground.</summary>
   internal static SolidColorBrush White { get; } = new(Colors.White);



   /// <summary>Text at the given opacity over the brand gradient.</summary>
   internal static Microsoft.UI.Xaml.Controls.TextBlock Text(string text, double size, double opacity = 1, bool strong = false)
       => new()
       {
          Text = text,
          FontSize = size,
          Foreground = White,
          Opacity = opacity,
          FontWeight = strong ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
          TextWrapping = TextWrapping.Wrap,
       };
}
