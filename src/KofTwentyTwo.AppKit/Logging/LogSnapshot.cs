/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

namespace KofTwentyTwo.AppKit.Logging;

/// <summary>The last lines of a log file and the file length they were read at.</summary>
/// <param name="Lines">The tail, oldest first.</param>
/// <param name="Length">File length in bytes when read; 0 when the file is absent.</param>
public sealed record LogSnapshot(IReadOnlyList<string> Lines, long Length)
{
   /// <summary>True when the input byte limit omitted a prefix of the file.</summary>
   public bool IsTruncated { get; init; }
}
