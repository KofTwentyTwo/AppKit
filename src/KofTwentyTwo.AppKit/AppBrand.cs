/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

namespace KofTwentyTwo.AppKit;

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
