/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Diagnostics.CodeAnalysis;
using Velopack;


namespace KofTwentyTwo.AppKit.Updates;

/// <summary>
/// Velopack's process hook. During install, update, and uninstall Velopack launches
/// the app's exe with special arguments; this handles them and may exit the process.
/// Outside those moments (F5, loose builds, MSIX) it does nothing.
/// </summary>
/// <remarks>
/// Retained for existing callers. For applications packaged by vpk, call
/// <c>VelopackApp.Build().Run()</c> directly in the entry assembly: vpk's static
/// startup verification cannot recognize this indirect wrapper.
/// </remarks>
public static class VelopackStartup
{
   /// <summary>
   /// Must be the first statement of Main, before any UI exists. WinUI apps therefore
   /// define DISABLE_XAML_GENERATED_MAIN and supply their own Program.Main.
   /// </summary>
   [ExcludeFromCodeCoverage(Justification = "Process-level hook; exercised by the release smoke test of an installed app.")]
   public static void Run() => VelopackApp.Build().Run();
}
