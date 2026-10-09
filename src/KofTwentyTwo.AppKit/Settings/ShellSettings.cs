using System.Text.Json.Serialization;


namespace KofTwentyTwo.AppKit.Settings;

/// <summary>The app's color theme.</summary>
public enum AppTheme
{
   /// <summary>Follow the Windows setting.</summary>
   System,

   /// <summary>Always light.</summary>
   Light,

   /// <summary>Always dark.</summary>
   Dark,
}



/// <summary>
/// Preferences every AppKit app shares: theme, startup splash, and automatic update
/// checks. Apps derive their own settings class from this one and register the derived
/// type in a source-generated JsonSerializerContext; override <see cref="Sanitize"/>
/// (calling the base) to repair app-specific values.
/// </summary>
public class ShellSettings : ISanitizable
{
   /// <summary>Shortest splash display time, in milliseconds.</summary>
   public const int MinSplashMilliseconds = 500;

   /// <summary>Longest splash display time, in milliseconds.</summary>
   public const int MaxSplashMilliseconds = 30_000;

   /// <summary>Default splash display time, in milliseconds.</summary>
   public const int DefaultSplashMilliseconds = 2_500;

   /// <summary>
   /// Stored theme name: "System", "Light", or "Dark". Kept as a string so one bad
   /// value is repaired by <see cref="Sanitize"/> instead of failing the whole file.
   /// </summary>
   public string Theme { get; set; } = nameof(AppTheme.System);



   /// <summary>The theme as an enum; unknown stored values read as <see cref="AppTheme.System"/>.</summary>
   [JsonIgnore]
   public AppTheme ThemeKind
   {
      get => Enum.TryParse(Theme, ignoreCase: false, out AppTheme theme) && Enum.IsDefined(theme) ? theme : AppTheme.System;
      set => Theme = value.ToString();
   }



   /// <summary>Whether the branded splash shows at startup.</summary>
   public bool ShowSplashScreen { get; set; } = true;

   /// <summary>How long the splash stays up, clamped to 500..30000 ms.</summary>
   public int SplashMilliseconds { get; set; } = DefaultSplashMilliseconds;

   /// <summary>Whether installed builds look for an update shortly after startup.</summary>
   public bool CheckForUpdatesOnStartup { get; set; } = true;



   /// <inheritdoc/>
   public virtual void Sanitize()
   {
      // The JSON deserializer can assign null despite the non-nullable annotation,
      // and numeric strings parse as enum values, so round-trip through the enum.
      Theme = ThemeKind.ToString();
      // 0 means the field was absent (written by an older version): use the default
      // rather than clamping an unset value to the minimum.
      SplashMilliseconds = SplashMilliseconds == 0
          ? DefaultSplashMilliseconds
          : Math.Clamp(SplashMilliseconds, MinSplashMilliseconds, MaxSplashMilliseconds);
   }
}
