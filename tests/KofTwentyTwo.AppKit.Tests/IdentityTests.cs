/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Reflection;
using System.Reflection.Emit;


namespace KofTwentyTwo.AppKit.Tests;

/// <summary>Tests for app identity: id validation, defaults, and derived names.</summary>
public class AppInfoTests
{
   /// <summary>Id: accepts valid ids.</summary>
   [Theory]
   [InlineData("gclo")]
   [InlineData("my-app-2")]
   [InlineData("9lives")]
   public void Id_AcceptsValidIds(string id)
   {
      var app = new AppInfo { Id = id, DisplayName = "X" };
      Assert.Equal(id, app.Id);
   }



   /// <summary>Id: rejects invalid ids.</summary>
   [Theory]
   [InlineData("")]
   [InlineData("Gclo")]
   [InlineData("-app")]
   [InlineData("my app")]
   [InlineData("my_app")]
   [InlineData(null)]
   public void Id_RejectsInvalidIds(string? id)
   {
      ArgumentException ex = Assert.Throws<ArgumentException>(() => new AppInfo { Id = id!, DisplayName = "X" });
      Assert.Equal("value", ex.ParamName);
      Assert.Contains("lowercase letters", ex.Message, StringComparison.Ordinal);
   }



   /// <summary>DataDirectoryVariable: is upper snake case of id.</summary>
   [Fact]
   public void DataDirectoryVariable_IsUpperSnakeCaseOfId()
   {
      Assert.Equal("MY_APP_DATA_DIR", new AppInfo { Id = "my-app", DisplayName = "X" }.DataDirectoryVariable);
   }



   /// <summary>Defaults: are empty and brand is set.</summary>
   [Fact]
   public void Defaults_AreEmptyAndBrandIsSet()
   {
      var app = new AppInfo { Id = "x", DisplayName = "X" };
      Assert.Equal("", app.Tagline);
      Assert.Equal("", app.Description);
      Assert.Null(app.RepositoryUrl);
      Assert.Equal("", app.Author);
      Assert.Equal("", app.AuthorEmail);
      Assert.Null(app.Website);
      Assert.Equal("MIT License", app.License);
      Assert.Equal("", app.Copyright);
      Assert.Empty(app.Attributions);
      Assert.Equal("#3B4CCA", app.Brand.GradientStart);
      Assert.Equal("#2A2F8F", app.Brand.GradientEnd);
      Assert.Equal("Assets/Brand/app-icon.svg", app.Brand.IconSvgPath);
      Assert.Equal("Assets/Brand/app.ico", app.Brand.IconIcoPath);
   }



   /// <summary>AllProperties: round trip.</summary>
   [Fact]
   public void AllProperties_RoundTrip()
   {
      var app = new AppInfo
      {
         Id = "x",
         DisplayName = "X",
         Tagline = "t",
         Description = "d",
         RepositoryUrl = new Uri("https://github.com/a/b"),
         Author = "a",
         AuthorEmail = "a@b.c",
         Website = new Uri("https://b.c"),
         License = "Apache-2.0",
         Copyright = "(c)",
         Brand = new AppBrand { GradientStart = "#000000", GradientEnd = "#FFFFFF", IconSvgPath = "a.svg", IconIcoPath = "a.ico" },
         Attributions = [new Attribution("Velopack", "MIT License")],
      };
      Assert.Equal("Velopack", app.Attributions[0].Name);
      Assert.Equal("MIT License", app.Attributions[0].License);
      Assert.Equal("a.ico", app.Brand.IconIcoPath);
      Assert.Equal(app, app with { });
   }
}



/// <summary>Tests for parsing brand colors.</summary>
public class ArgbColorTests
{
   /// <summary>Parse: six digits is opaque.</summary>
   [Fact]
   public void Parse_SixDigits_IsOpaque()
   {
      Assert.Equal(new ArgbColor(0xFF, 0x3B, 0x4C, 0xCA), ArgbColor.Parse("#3B4CCA"));
   }



   /// <summary>Parse: eight digits keeps alpha.</summary>
   [Fact]
   public void Parse_EightDigits_KeepsAlpha()
   {
      Assert.Equal(new ArgbColor(0x33, 0xFF, 0xFF, 0xFF), ArgbColor.Parse("#33ffffff"));
   }



   /// <summary>Components: are exposed.</summary>
   [Fact]
   public void Components_AreExposed()
   {
      var color = ArgbColor.Parse("#01020304");
      Assert.Equal((1, 2, 3, 4), (color.A, color.R, color.G, color.B));
   }



   /// <summary>TryParse: rejects malformed.</summary>
   [Theory]
   [InlineData(null)]
   [InlineData("")]
   [InlineData("3B4CCA1")]
   [InlineData("#3B4CC")]
   [InlineData("#GGGGGG")]
   public void TryParse_RejectsMalformed(string? text)
   {
      Assert.False(ArgbColor.TryParse(text, out ArgbColor color));
      Assert.Equal(default, color);
   }



   /// <summary>Parse: malformed throws.</summary>
   [Fact]
   public void Parse_Malformed_Throws()
   {
      FormatException error = Assert.Throws<FormatException>(() => ArgbColor.Parse("red"));
      Assert.Contains("red", error.Message, StringComparison.Ordinal);
      Assert.Contains("#RRGGBB", error.Message, StringComparison.Ordinal);
   }
}



/// <summary>Tests for the per-user data paths and the data-directory override.</summary>
public class AppPathsTests
{
   private static readonly AppInfo s_app = new() { Id = "my-app", DisplayName = "My App" };



   /// <summary>DataRoot: defaults to local app data.</summary>
   [Fact]
   public void DataRoot_DefaultsToLocalAppData()
   {
      var paths = new AppPaths(s_app, _ => null);
      string expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "my-app");
      Assert.Equal(expected, paths.DataRoot);
   }



   /// <summary>DataRoot: empty override uses default.</summary>
   [Fact]
   public void DataRoot_EmptyOverride_UsesDefault()
   {
      var paths = new AppPaths(s_app, _ => "");
      Assert.EndsWith("my-app", paths.DataRoot, StringComparison.Ordinal);
   }



   /// <summary>DataRoot: honors override variable.</summary>
   [Fact]
   public void DataRoot_HonorsOverrideVariable()
   {
      string? asked = null;
      var paths = new AppPaths(s_app, name =>
      {
         asked = name;
         return @"D:\portable";
      });
      Assert.Equal(@"D:\portable", paths.DataRoot);
      Assert.Equal("MY_APP_DATA_DIR", asked);
      Assert.Equal(@"D:\portable\logs", paths.LogsDirectory);
      Assert.Equal(@"D:\portable\settings.json", paths.SettingsFile);
      Assert.Equal(@"D:\portable\accounts.json", paths.GetFile("accounts.json"));
   }



   /// <summary>PublicConstructor: reads the real environment.</summary>
   [Fact]
   public void PublicConstructor_ReadsTheRealEnvironment()
   {
      var unique = new AppInfo { Id = "appkit-test-" + Guid.NewGuid().ToString("N"), DisplayName = "X" };
      Assert.EndsWith(unique.Id, new AppPaths(unique).DataRoot, StringComparison.Ordinal);
   }



   /// <summary>Constructor: null app throws.</summary>
   [Fact]
   public void Constructor_NullApp_Throws()
   {
      Assert.Throws<ArgumentNullException>(() => new AppPaths(null!));
   }
}



/// <summary>Tests for the build identity text and prerelease detection.</summary>
public class BuildVersionTests
{
   /// <summary>Format: shortens commit metadata.</summary>
   [Theory]
   [InlineData(null, "unknown")]
   [InlineData("  ", "unknown")]
   [InlineData("1.2.3", "1.2.3")]
   [InlineData("1.2.3-beta.1+abc", "1.2.3-beta.1 (abc)")]
   [InlineData("1.2.3+0123456789abcdef", "1.2.3 (012345678)")]
   [InlineData("1.2.3+abc.more", "1.2.3 (abc)")]
   [InlineData("1.2.3+", "1.2.3")]
   [InlineData("+abc", " (abc)")]
   [InlineData("1.2.3+.more", "1.2.3")]
   public void Format_ShortensCommitMetadata(string? input, string expected)
   {
      Assert.Equal(expected, BuildVersion.Format(input));
   }



   /// <summary>IsPrerelease: looks only at the version part.</summary>
   [Theory]
   [InlineData(null, false)]
   [InlineData("1.2.3", false)]
   [InlineData("1.2.3+sha-with-dash", false)]
   [InlineData("+sha-with-dash", false)]
   [InlineData("1.2.3-dev", true)]
   [InlineData("1.2.3-beta.1+abc", true)]
   public void IsPrerelease_LooksOnlyAtTheVersionPart(string? input, bool expected)
   {
      Assert.Equal(expected, BuildVersion.IsPrerelease(input));
   }



   /// <summary>Describe: reads informational version.</summary>
   [Fact]
   public void Describe_ReadsInformationalVersion()
   {
      Assembly assembly = Emit(informationalVersion: "4.5.6-rc.1+deadbeef", version: null);
      Assert.Equal("4.5.6-rc.1 (deadbeef)", BuildVersion.Describe(assembly));
      Assert.True(BuildVersion.IsPrerelease(assembly));
   }



   /// <summary>Describe: falls back to assembly version.</summary>
   [Fact]
   public void Describe_FallsBackToAssemblyVersion()
   {
      Assembly assembly = Emit(informationalVersion: null, version: new Version(1, 2, 3, 4));
      Assert.Equal("1.2.3.4", BuildVersion.Describe(assembly));
      Assert.False(BuildVersion.IsPrerelease(assembly));
   }



   /// <summary>Describe: no version attributes is zero version.</summary>
   [Fact]
   public void Describe_NoVersionAttributes_IsZeroVersion()
   {
      Assembly assembly = Emit(informationalVersion: null, version: null);
      Assert.Equal("0.0.0.0", BuildVersion.Describe(assembly));
      Assert.False(BuildVersion.IsPrerelease(assembly));
   }



   /// <summary>NullAssembly: throws.</summary>
   [Fact]
   public void NullAssembly_Throws()
   {
      Assert.Equal("assembly", Assert.Throws<ArgumentNullException>(() => BuildVersion.Describe(null!)).ParamName);
      Assert.Equal("assembly", Assert.Throws<ArgumentNullException>(() => BuildVersion.IsPrerelease((Assembly)null!)).ParamName);
   }



   /// <summary>Emits an in-memory assembly with the given informational and assembly versions, so every fallback branch can be tested.</summary>
   private static AssemblyBuilder Emit(string? informationalVersion, Version? version)
   {
      var name = new AssemblyName("Emitted" + Guid.NewGuid().ToString("N")) { Version = version };
      var builder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
      if(informationalVersion is not null)
      {
         ConstructorInfo ctor = typeof(AssemblyInformationalVersionAttribute).GetConstructor([typeof(string)])!;
         builder.SetCustomAttribute(new CustomAttributeBuilder(ctor, [informationalVersion]));
      }
      return builder;
   }
}
