using System.Text.RegularExpressions;


namespace KofTwentyTwo.AppKit;

/// <summary>
/// Identity of the host application, declared once at startup and handed to every
/// AppKit component that needs to know whose app it is running in (data paths, log
/// file names, credential names, About and splash screens, update prompts).
/// </summary>
public sealed partial record AppInfo
{
   private readonly string _id = "";



   /// <summary>
   /// Machine name of the app: lowercase letters, digits, and dashes, starting with a
   /// letter or digit (e.g. "gclo"). Used for the data folder, log file prefix,
   /// credential target prefix, and the data-directory override variable.
   /// </summary>
   /// <exception cref="ArgumentException">The value is not a valid app id.</exception>
   public required string Id
   {
      get => _id;
      init
      {
         if(value is null || !IdPattern().IsMatch(value))
         {
            throw new ArgumentException(
                $"App id '{value}' is invalid: use lowercase letters, digits, and dashes, starting with a letter or digit.",
                nameof(Id));
         }
         _id = value;
      }
   }



   /// <summary>Human-readable product name shown in titles and dialogs.</summary>
   public required string DisplayName { get; init; }

   /// <summary>Short subtitle shown under the name on the splash and About screens.</summary>
   public string Tagline { get; init; } = "";

   /// <summary>One- or two-sentence description shown in the About screen.</summary>
   public string Description { get; init; } = "";

   /// <summary>Public source repository; also the default self-update feed.</summary>
   public Uri? RepositoryUrl { get; init; }

   /// <summary>Author name shown in credits.</summary>
   public string Author { get; init; } = "";

   /// <summary>Author contact address shown in credits.</summary>
   public string AuthorEmail { get; init; } = "";

   /// <summary>Author or product website shown in credits.</summary>
   public Uri? Website { get; init; }

   /// <summary>License name of the app itself, e.g. "MIT License".</summary>
   public string License { get; init; } = "MIT License";

   /// <summary>Copyright line, e.g. "Copyright (c) 2026 James Maes".</summary>
   public string Copyright { get; init; } = "";

   /// <summary>Colors and icon locations for branded surfaces.</summary>
   public AppBrand Brand { get; init; } = new();

   /// <summary>Third-party components credited in the About screen.</summary>
   public IReadOnlyList<Attribution> Attributions { get; init; } = [];



   /// <summary>
   /// Environment variable that relocates the app's per-user data folder, e.g.
   /// "GCLO_DATA_DIR" for id "gclo". UI tests and portable setups use it to isolate
   /// the app from the real profile.
   /// </summary>
   public string DataDirectoryVariable
       => Id.ToUpperInvariant().Replace('-', '_') + "_DATA_DIR";



   [GeneratedRegex("^[a-z0-9][a-z0-9-]*$")]
   private static partial Regex IdPattern();
}



/// <summary>A third-party component credited in the About screen.</summary>
/// <param name="Name">Component name, e.g. "Velopack".</param>
/// <param name="License">License name, e.g. "MIT License".</param>
public sealed record Attribution(string Name, string License);



/// <summary>
/// Brand look for the splash and About header: a diagonal two-stop gradient behind
/// white type, plus the app-relative paths of the vector mark and window icon.
/// </summary>
public sealed record AppBrand
{
   /// <summary>Top-left gradient color, "#RRGGBB" or "#AARRGGBB".</summary>
   public string GradientStart { get; init; } = "#3B4CCA";

   /// <summary>Bottom-right gradient color, "#RRGGBB" or "#AARRGGBB".</summary>
   public string GradientEnd { get; init; } = "#2A2F8F";

   /// <summary>Vector mark, relative to the app directory.</summary>
   public string IconSvgPath { get; init; } = "Assets/Brand/app-icon.svg";

   /// <summary>Window and taskbar icon, relative to the app directory.</summary>
   public string IconIcoPath { get; init; } = "Assets/Brand/app.ico";
}
