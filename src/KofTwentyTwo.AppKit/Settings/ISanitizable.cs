/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

namespace KofTwentyTwo.AppKit.Settings;

/// <summary>Settings that repair out-of-range or missing values after loading.</summary>
public interface ISanitizable
{
   /// <summary>Clamps, defaults, and validates every value in place.</summary>
   void Sanitize();
}
