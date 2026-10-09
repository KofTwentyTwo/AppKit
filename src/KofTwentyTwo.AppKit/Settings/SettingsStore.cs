using System.Text.Json;
using System.Text.Json.Serialization.Metadata;


namespace KofTwentyTwo.AppKit.Settings;

/// <summary>Settings that repair out-of-range or missing values after loading.</summary>
public interface ISanitizable
{
   /// <summary>Clamps, defaults, and validates every value in place.</summary>
   void Sanitize();
}



/// <summary>
/// Loads and saves a settings object as indented JSON. Takes a source-generated
/// <see cref="JsonTypeInfo{T}"/> so trimmed Release publishes need no reflection.
/// Neither operation ever throws: preferences must never prevent startup or crash
/// the app. Saves write a temporary file and swap it in, so a crash mid-save cannot
/// leave a truncated settings file behind.
/// </summary>
/// <typeparam name="T">The settings type; implement <see cref="ISanitizable"/> to repair values on load and save.</typeparam>
public sealed class SettingsStore<T>
    where T : class, new()
{
   private readonly JsonTypeInfo<T> _typeInfo;



   /// <summary>Creates a store for the file at <paramref name="path"/>.</summary>
   public SettingsStore(string path, JsonTypeInfo<T> typeInfo)
   {
      ArgumentException.ThrowIfNullOrWhiteSpace(path);
      ArgumentNullException.ThrowIfNull(typeInfo);
      Path = path;
      _typeInfo = typeInfo;
   }



   /// <summary>The settings file.</summary>
   public string Path { get; }



   /// <summary>
   /// Loads settings. A missing, corrupt, or unreadable file yields defaults; loaded
   /// values are sanitized.
   /// </summary>
   public T Load()
   {
      T? loaded = null;
      try
      {
         if(File.Exists(Path))
         {
            loaded = JsonSerializer.Deserialize(File.ReadAllText(Path), _typeInfo);
         }
      }
      catch
      {
         // Settings must never prevent startup; fall through to defaults.
      }

      loaded ??= new T();
      (loaded as ISanitizable)?.Sanitize();
      return loaded;
   }



   /// <summary>
   /// Sanitizes and persists <paramref name="settings"/>, creating the folder if
   /// needed. Returns false when the write failed (locked file, read-only profile,
   /// full disk); losing a preference write must never crash the app.
   /// </summary>
   public bool Save(T settings)
   {
      ArgumentNullException.ThrowIfNull(settings);
      string temporary = Path + ".tmp";
      try
      {
         (settings as ISanitizable)?.Sanitize();
         string? directory = System.IO.Path.GetDirectoryName(Path);
         if(!string.IsNullOrEmpty(directory))
         {
            Directory.CreateDirectory(directory);
         }
         File.WriteAllText(temporary, JsonSerializer.Serialize(settings, _typeInfo));
         File.Move(temporary, Path, overwrite: true);
         return true;
      }
      catch
      {
         TryDelete(temporary);
         return false;
      }
   }



   private static void TryDelete(string path)
   {
      try
      {
         File.Delete(path);
      }
      catch
      {
         // Best effort: a leftover .tmp is harmless and overwritten next save.
      }
   }
}
