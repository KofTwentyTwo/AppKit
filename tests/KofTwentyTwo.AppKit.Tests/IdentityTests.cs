using System.Reflection;
using System.Reflection.Emit;

namespace KofTwentyTwo.AppKit.Tests;

public class AppInfoTests
{
    [Theory]
    [InlineData("gclo")]
    [InlineData("my-app-2")]
    [InlineData("9lives")]
    public void Id_AcceptsValidIds(string id)
    {
        var app = new AppInfo { Id = id, DisplayName = "X" };
        Assert.Equal(id, app.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Gclo")]
    [InlineData("-app")]
    [InlineData("my app")]
    [InlineData("my_app")]
    [InlineData(null)]
    public void Id_RejectsInvalidIds(string? id)
    {
        var ex = Assert.Throws<ArgumentException>(() => new AppInfo { Id = id!, DisplayName = "X" });
        Assert.Equal("Id", ex.ParamName);
    }

    [Fact]
    public void DataDirectoryVariable_IsUpperSnakeCaseOfId()
    {
        Assert.Equal("MY_APP_DATA_DIR", new AppInfo { Id = "my-app", DisplayName = "X" }.DataDirectoryVariable);
    }

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

public class ArgbColorTests
{
    [Fact]
    public void Parse_SixDigits_IsOpaque()
    {
        Assert.Equal(new ArgbColor(0xFF, 0x3B, 0x4C, 0xCA), ArgbColor.Parse("#3B4CCA"));
    }

    [Fact]
    public void Parse_EightDigits_KeepsAlpha()
    {
        Assert.Equal(new ArgbColor(0x33, 0xFF, 0xFF, 0xFF), ArgbColor.Parse("#33ffffff"));
    }

    [Fact]
    public void Components_AreExposed()
    {
        ArgbColor color = ArgbColor.Parse("#01020304");
        Assert.Equal((1, 2, 3, 4), (color.A, color.R, color.G, color.B));
    }

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

    [Fact]
    public void Parse_Malformed_Throws()
    {
        Assert.Throws<FormatException>(() => ArgbColor.Parse("red"));
    }
}

public class AppPathsTests
{
    private static readonly AppInfo App = new() { Id = "my-app", DisplayName = "My App" };

    [Fact]
    public void DataRoot_DefaultsToLocalAppData()
    {
        var paths = new AppPaths(App, _ => null);
        string expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "my-app");
        Assert.Equal(expected, paths.DataRoot);
    }

    [Fact]
    public void DataRoot_EmptyOverride_UsesDefault()
    {
        var paths = new AppPaths(App, _ => "");
        Assert.EndsWith("my-app", paths.DataRoot, StringComparison.Ordinal);
    }

    [Fact]
    public void DataRoot_HonorsOverrideVariable()
    {
        string? asked = null;
        var paths = new AppPaths(App, name =>
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

    [Fact]
    public void PublicConstructor_ReadsTheRealEnvironment()
    {
        var unique = new AppInfo { Id = "appkit-test-" + Guid.NewGuid().ToString("N"), DisplayName = "X" };
        Assert.EndsWith(unique.Id, new AppPaths(unique).DataRoot, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_NullApp_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AppPaths(null!));
    }
}

public class BuildVersionTests
{
    [Theory]
    [InlineData(null, "unknown")]
    [InlineData("  ", "unknown")]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("1.2.3-beta.1+abc", "1.2.3-beta.1 (abc)")]
    [InlineData("1.2.3+0123456789abcdef", "1.2.3 (012345678)")]
    [InlineData("1.2.3+abc.more", "1.2.3 (abc)")]
    [InlineData("1.2.3+", "1.2.3")]
    public void Format_ShortensCommitMetadata(string? input, string expected)
    {
        Assert.Equal(expected, BuildVersion.Format(input));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("1.2.3", false)]
    [InlineData("1.2.3+sha-with-dash", false)]
    [InlineData("1.2.3-dev", true)]
    [InlineData("1.2.3-beta.1+abc", true)]
    public void IsPrerelease_LooksOnlyAtTheVersionPart(string? input, bool expected)
    {
        Assert.Equal(expected, BuildVersion.IsPrerelease(input));
    }

    [Fact]
    public void Describe_ReadsInformationalVersion()
    {
        Assembly assembly = Emit(informationalVersion: "4.5.6-rc.1+deadbeef", version: null);
        Assert.Equal("4.5.6-rc.1 (deadbeef)", BuildVersion.Describe(assembly));
        Assert.True(BuildVersion.IsPrerelease(assembly));
    }

    [Fact]
    public void Describe_FallsBackToAssemblyVersion()
    {
        Assembly assembly = Emit(informationalVersion: null, version: new Version(1, 2, 3, 4));
        Assert.Equal("1.2.3.4", BuildVersion.Describe(assembly));
        Assert.False(BuildVersion.IsPrerelease(assembly));
    }

    [Fact]
    public void Describe_NoVersionAttributes_IsZeroVersion()
    {
        Assembly assembly = Emit(informationalVersion: null, version: null);
        Assert.Equal("0.0.0.0", BuildVersion.Describe(assembly));
        Assert.False(BuildVersion.IsPrerelease(assembly));
    }

    [Fact]
    public void NullAssembly_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BuildVersion.Describe(null!));
        Assert.Throws<ArgumentNullException>(() => BuildVersion.IsPrerelease((Assembly)null!));
    }

    private static Assembly Emit(string? informationalVersion, Version? version)
    {
        var name = new AssemblyName("Emitted" + Guid.NewGuid().ToString("N")) { Version = version };
        AssemblyBuilder builder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        if (informationalVersion is not null)
        {
            ConstructorInfo ctor = typeof(AssemblyInformationalVersionAttribute).GetConstructor([typeof(string)])!;
            builder.SetCustomAttribute(new CustomAttributeBuilder(ctor, [informationalVersion]));
        }
        return builder;
    }
}
