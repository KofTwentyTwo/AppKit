/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

namespace KofTwentyTwo.AppKit;

/// <summary>
/// The single seam for where an app keeps its per-user data (settings, logs, any
/// app-specific files). Defaults to %LOCALAPPDATA%\&lt;id&gt;; the
/// <see cref="AppInfo.DataDirectoryVariable"/> environment variable overrides it so UI
/// tests and portable setups can point the whole app at an isolated directory.
/// Uses Environment.GetFolderPath, not Windows.Storage.ApplicationData, so the same
/// path works packaged and unpackaged.
/// </summary>
public sealed class AppPaths
{
   private readonly AppInfo _app;
   private readonly Func<string, string?> _getEnvironmentVariable;



   /// <summary>Creates the paths for <paramref name="app"/>.</summary>
   public AppPaths(AppInfo app)
       : this(app, Environment.GetEnvironmentVariable)
   {
   }



   /// <summary>Test seam: the environment lookup is injectable.</summary>
   internal AppPaths(AppInfo app, Func<string, string?> getEnvironmentVariable)
   {
      ArgumentNullException.ThrowIfNull(app);
      _app = app;
      _getEnvironmentVariable = getEnvironmentVariable;
   }



   /// <summary>
   /// Root of the app's per-user data. Read on every access so a value set before
   /// launch always wins over any cached default.
   /// </summary>
   public string DataRoot
       => _getEnvironmentVariable(_app.DataDirectoryVariable) is { Length: > 0 } dir
           ? dir
           : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), _app.Id);



   /// <summary>Folder for the daily activity log files.</summary>
   public string LogsDirectory => Path.Combine(DataRoot, "logs");

   /// <summary>The user-preferences file.</summary>
   public string SettingsFile => Path.Combine(DataRoot, "settings.json");



   /// <summary>Any other file under <see cref="DataRoot"/>, e.g. "accounts.json".</summary>
   public string GetFile(string relativePath) => Path.Combine(DataRoot, relativePath);
}
