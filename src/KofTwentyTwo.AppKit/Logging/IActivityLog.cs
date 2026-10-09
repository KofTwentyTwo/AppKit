/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

namespace KofTwentyTwo.AppKit.Logging;

/// <summary>
/// Application activity log for human-readable operational events. Implementations
/// must be safe to call from any thread and must never throw from a logging call;
/// secrets (tokens, passwords) must never be written to it.
/// </summary>
public interface IActivityLog
{
   /// <summary>Records an informational event.</summary>
   void Info(string message);



   /// <summary>Records something unexpected that did not stop the operation.</summary>
   void Warning(string message);



   /// <summary>Records a failure, with the exception's full text when one is given.</summary>
   void Error(string message, Exception? exception = null);



   /// <summary>Folder that holds the log files.</summary>
   string LogDirectory { get; }

   /// <summary>Full path of the file new entries are currently appended to.</summary>
   string CurrentLogFilePath { get; }
}



/// <summary>An <see cref="IActivityLog"/> that discards everything.</summary>
public sealed class NullActivityLog : IActivityLog
{
   /// <summary>The shared instance.</summary>
   public static NullActivityLog Instance { get; } = new();

   /// <inheritdoc/>
   public string LogDirectory => "";

   /// <inheritdoc/>
   public string CurrentLogFilePath => "";



   /// <inheritdoc/>
   public void Info(string message)
   {
   }



   /// <inheritdoc/>
   public void Warning(string message)
   {
   }



   /// <inheritdoc/>
   public void Error(string message, Exception? exception = null)
   {
   }
}
