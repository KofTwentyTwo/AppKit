/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;


namespace KofTwentyTwo.AppKit.Logging;

/// <summary>Adapts existing activity-log implementations to standard structured .NET logging.</summary>
public static class ActivityLogExtensions
{
   /// <summary>
   /// Creates a logger that records templates, fields, event IDs and scopes as JSON.
   /// Keep and reuse the returned logger to share scopes. Existing activity-log
   /// implementations need no new members; their writes retain the JSON record.
   /// </summary>
   public static ILogger AsLogger(this IActivityLog log)
   {
      ArgumentNullException.ThrowIfNull(log);
      return log is NullActivityLog ? NullLogger.Instance : new ActivityLogLogger(log);
   }
}
