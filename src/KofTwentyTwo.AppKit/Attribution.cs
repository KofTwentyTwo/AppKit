/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

namespace KofTwentyTwo.AppKit;

/// <summary>A third-party component credited in the About screen.</summary>
/// <param name="Name">Component name, e.g. "Velopack".</param>
/// <param name="License">License name, e.g. "MIT License".</param>
public sealed record Attribution(string Name, string License);
