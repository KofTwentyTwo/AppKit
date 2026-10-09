using System.Text.Json.Serialization;
using KofTwentyTwo.AppKit.Settings;

namespace KofTwentyTwo.AppKit.Tests;

/// <summary>An app's settings, the way apps are meant to extend <see cref="ShellSettings"/>.</summary>
public sealed class SampleSettings : ShellSettings
{
    public int Concurrency { get; set; } = 8;

    public override void Sanitize()
    {
        base.Sanitize();
        Concurrency = Math.Clamp(Concurrency, 1, 64);
    }
}

/// <summary>A settings type that does not sanitize.</summary>
public sealed class PlainSettings
{
    public string Name { get; set; } = "default";
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SampleSettings))]
[JsonSerializable(typeof(PlainSettings))]
internal sealed partial class TestSettingsContext : JsonSerializerContext
{
}

public sealed class SettingsStoreTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    private SettingsStore<SampleSettings> Store(string? path = null)
        => new(path ?? _temp.File(Path.Combine("nested", "settings.json")), TestSettingsContext.Default.SampleSettings);

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        SampleSettings settings = Store().Load();
        Assert.Equal(8, settings.Concurrency);
        Assert.Equal(AppTheme.System, settings.ThemeKind);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        SettingsStore<SampleSettings> store = Store();
        var settings = new SampleSettings { Concurrency = 12, ThemeKind = AppTheme.Dark, ShowSplashScreen = false, SplashMilliseconds = 1000, CheckForUpdatesOnStartup = false };
        Assert.True(store.Save(settings));
        Assert.False(File.Exists(store.Path + ".tmp"));

        SampleSettings loaded = store.Load();
        Assert.Equal(12, loaded.Concurrency);
        Assert.Equal(AppTheme.Dark, loaded.ThemeKind);
        Assert.Equal("Dark", loaded.Theme);
        Assert.False(loaded.ShowSplashScreen);
        Assert.Equal(1000, loaded.SplashMilliseconds);
        Assert.False(loaded.CheckForUpdatesOnStartup);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("null")]
    public void Load_CorruptOrNullFile_ReturnsDefaults(string content)
    {
        SettingsStore<SampleSettings> store = Store();
        Directory.CreateDirectory(Path.GetDirectoryName(store.Path)!);
        File.WriteAllText(store.Path, content);
        Assert.Equal(8, store.Load().Concurrency);
    }

    [Fact]
    public void Load_SanitizesOutOfRangeValues()
    {
        SettingsStore<SampleSettings> store = Store();
        Directory.CreateDirectory(Path.GetDirectoryName(store.Path)!);
        File.WriteAllText(store.Path, """{ "Theme": "Purple", "SplashMilliseconds": 99999999, "Concurrency": 500 }""");
        SampleSettings loaded = store.Load();
        Assert.Equal("System", loaded.Theme);
        Assert.Equal(ShellSettings.MaxSplashMilliseconds, loaded.SplashMilliseconds);
        Assert.Equal(64, loaded.Concurrency);
    }

    [Fact]
    public void Save_FailureReturnsFalse()
    {
        string notADirectory = _temp.File("file");
        File.WriteAllText(notADirectory, "");
        Assert.False(Store(Path.Combine(notADirectory, "settings.json")).Save(new SampleSettings()));
    }

    [Fact]
    public void Save_SwapFails_CleansUpTheTemporaryFile()
    {
        SettingsStore<SampleSettings> store = Store();
        Directory.CreateDirectory(store.Path); // the rename target is a directory
        Assert.False(store.Save(new SampleSettings()));
        Assert.False(File.Exists(store.Path + ".tmp"));
    }

    [Fact]
    public void Save_TemporaryPathBlocked_ReturnsFalseAndLeavesOriginal()
    {
        SettingsStore<SampleSettings> store = Store();
        Assert.True(store.Save(new SampleSettings { Concurrency = 3 }));
        // A read-only leftover on the temp name: the write fails and so does the cleanup.
        string temporary = store.Path + ".tmp";
        File.WriteAllText(temporary, "stale");
        File.SetAttributes(temporary, FileAttributes.ReadOnly);
        try
        {
            Assert.False(store.Save(new SampleSettings { Concurrency = 4 }));
            Assert.Equal(3, store.Load().Concurrency);
        }
        finally
        {
            File.SetAttributes(temporary, FileAttributes.Normal);
        }
    }

    [Fact]
    public void Save_BareFileName_WritesRelativeToCurrentDirectory()
    {
        string name = $"appkit-{Guid.NewGuid():N}.json";
        try
        {
            Assert.True(new SettingsStore<PlainSettings>(name, TestSettingsContext.Default.PlainSettings).Save(new PlainSettings { Name = "n" }));
            Assert.Equal("n", new SettingsStore<PlainSettings>(name, TestSettingsContext.Default.PlainSettings).Load().Name);
        }
        finally
        {
            File.Delete(name);
        }
    }

    [Fact]
    public void Constructor_And_Save_ValidateArguments()
    {
        Assert.Throws<ArgumentException>(() => new SettingsStore<PlainSettings>(" ", TestSettingsContext.Default.PlainSettings));
        Assert.Throws<ArgumentNullException>(() => new SettingsStore<PlainSettings>("a.json", null!));
        Assert.Throws<ArgumentNullException>(() => Store().Save(null!));
    }
}

public class ShellSettingsTests
{
    [Theory]
    [InlineData("Light", AppTheme.Light)]
    [InlineData("Dark", AppTheme.Dark)]
    [InlineData("System", AppTheme.System)]
    [InlineData("dark", AppTheme.System)]
    [InlineData("42", AppTheme.System)]
    [InlineData(null, AppTheme.System)]
    public void ThemeKind_ToleratesBadStoredValues(string? stored, AppTheme expected)
    {
        Assert.Equal(expected, new ShellSettings { Theme = stored! }.ThemeKind);
    }

    [Theory]
    [InlineData(0, ShellSettings.DefaultSplashMilliseconds)]
    [InlineData(1, ShellSettings.MinSplashMilliseconds)]
    [InlineData(1500, 1500)]
    [InlineData(int.MaxValue, ShellSettings.MaxSplashMilliseconds)]
    public void Sanitize_ClampsSplash(int stored, int expected)
    {
        var settings = new ShellSettings { SplashMilliseconds = stored, Theme = null! };
        settings.Sanitize();
        Assert.Equal(expected, settings.SplashMilliseconds);
        Assert.Equal("System", settings.Theme);
    }

    [Fact]
    public void Defaults()
    {
        var settings = new ShellSettings();
        Assert.True(settings.ShowSplashScreen);
        Assert.True(settings.CheckForUpdatesOnStartup);
        Assert.Equal(ShellSettings.DefaultSplashMilliseconds, settings.SplashMilliseconds);
    }
}
