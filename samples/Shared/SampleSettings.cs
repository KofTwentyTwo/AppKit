/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Settings;


namespace AppKit.Sample;

/// <summary>The sample's preferences: the shared shell settings plus one of its own.</summary>
internal sealed class SampleSettings : ShellSettings
{
   public string Greeting { get; set; } = "Hello from AppKit";



   /// <summary>Repairs the shared shell settings, then restores a missing greeting.</summary>
   public override void Sanitize()
   {
      base.Sanitize();
      Greeting ??= "Hello from AppKit";
   }
}
