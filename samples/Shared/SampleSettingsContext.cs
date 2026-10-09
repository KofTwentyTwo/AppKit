/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json.Serialization;


namespace AppKit.Sample;

/// <summary>Source-generated JSON serializer for the sample&apos;s settings, so trimmed builds need no reflection.</summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SampleSettings))]
internal sealed partial class SampleSettingsContext : JsonSerializerContext
{
}
