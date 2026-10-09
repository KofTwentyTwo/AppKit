using System.Globalization;
using System.Runtime.InteropServices;


namespace KofTwentyTwo.AppKit;

/// <summary>
/// A framework-neutral color parsed from "#RRGGBB" or "#AARRGGBB", so brand colors can
/// be declared once in <see cref="AppBrand"/> and converted by each UI package.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct ArgbColor(byte A, byte R, byte G, byte B)
{
   /// <summary>Parses "#RRGGBB" (opaque) or "#AARRGGBB".</summary>
   /// <exception cref="FormatException">The text is not a supported hex color.</exception>
   public static ArgbColor Parse(string text)
       => TryParse(text, out ArgbColor color)
           ? color
           : throw new FormatException($"'{text}' is not a color; expected #RRGGBB or #AARRGGBB.");



   /// <summary>Parses "#RRGGBB" (opaque) or "#AARRGGBB"; false when malformed.</summary>
   public static bool TryParse(string? text, out ArgbColor color)
   {
      color = default;
      if(text is null || text.Length is not (7 or 9) || text[0] != '#'
          || !uint.TryParse(text.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint value))
      {
         return false;
      }

      if(text.Length == 7)
      {
         value |= 0xFF000000;
      }
      color = new ArgbColor((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);
      return true;
   }
}
