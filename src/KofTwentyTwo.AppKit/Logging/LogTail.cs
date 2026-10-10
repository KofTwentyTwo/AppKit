/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Text;
using System.Text.RegularExpressions;


namespace KofTwentyTwo.AppKit.Logging;
/// <summary>
/// Reads and filters <see cref="FileActivityLog"/> files for the log viewer windows.
/// Never throws: this is diagnostics UI, and an unreadable file becomes a readable
/// message instead.
/// </summary>
public static partial class LogTail
{
   /// <summary>Default number of trailing lines a viewer shows.</summary>
   public const int DefaultMaxLines = 2000;



   /// <summary>Maximum trailing input per read; also bounds a single decoded line.</summary>
   public const int DefaultMaxBytes = 1024 * 1024;



   /// <summary>
   /// The last <paramref name="maxLines"/> lines of <paramref name="path"/>, opened
   /// shared so the logger can keep appending while it is read.
   /// </summary>
   public static LogSnapshot Read(string? path, int maxLines = DefaultMaxLines)
      => Read(path, maxLines, DefaultMaxBytes);



   /// <summary>
   /// Reads at most <paramref name="maxBytes"/> trailing bytes plus a four-byte encoding
   /// header. UTF-8 and BOM-marked UTF-16/UTF-32 are supported. A skipped prefix may
   /// leave a partial first line; <see cref="LogSnapshot.IsTruncated"/> identifies it.
   /// The byte budget must be between four bytes and <see cref="DefaultMaxBytes"/>.
   /// </summary>
   public static LogSnapshot Read(string? path, int maxLines, int maxBytes)
   {
      ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLines);
      ArgumentOutOfRangeException.ThrowIfLessThan(maxBytes, 4);
      ArgumentOutOfRangeException.ThrowIfGreaterThan(maxBytes, DefaultMaxBytes);
      try
      {
         if(string.IsNullOrWhiteSpace(path) || !File.Exists(path))
         {
            return new LogSnapshot([], 0);
         }

         using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 1);
         long length = stream.Length;
         Span<byte> header = stackalloc byte[4];
         int headerLength = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
         (Encoding encoding, int preamble, int width) = DetectEncoding(header[..headerLength]);
         long start = Math.Max(preamble, length - maxBytes);
         // Round forward to a code-unit boundary without exceeding the input budget.
         start += (width - ((start - preamble) % width)) % width;
         stream.Position = start;
         byte[] bytes = new byte[(int)(length - start)];
         stream.ReadExactly(bytes);
         using var reader = new StringReader(encoding.GetString(bytes));

         var tail = new Queue<string>(Math.Min(maxLines, DefaultMaxLines));
         while(reader.ReadLine() is string line)
         {
            if(tail.Count == maxLines)
            {
               tail.Dequeue();
            }
            tail.Enqueue(line);
         }
         return new LogSnapshot([.. tail], length) { IsTruncated = start > preamble };
      }
      catch(Exception ex)
      {
         return new LogSnapshot(["Could not read the log file.", ex.Message], 0);
      }
   }



   /// <summary>Preserves the BOM detection supported by StreamReader, including UTF-32 before UTF-16.</summary>
   private static (Encoding Encoding, int Preamble, int Width) DetectEncoding(ReadOnlySpan<byte> header)
   {
      if(header.StartsWith<byte>([0xFF, 0xFE, 0x00, 0x00]))
      {
         return (Encoding.UTF32, 4, 4);
      }
      if(header.StartsWith<byte>([0x00, 0x00, 0xFE, 0xFF]))
      {
         return (new UTF32Encoding(bigEndian: true, byteOrderMark: true), 4, 4);
      }
      if(header.StartsWith<byte>([0xFF, 0xFE]))
      {
         return (Encoding.Unicode, 2, 2);
      }
      if(header.StartsWith<byte>([0xFE, 0xFF]))
      {
         return (Encoding.BigEndianUnicode, 2, 2);
      }
      return header.StartsWith<byte>([0xEF, 0xBB, 0xBF]) ? (Encoding.UTF8, 3, 1) : (Encoding.UTF8, 0, 1);
   }



   /// <summary>
   /// Keeps [ERROR] entries including their continuation lines: exception text spans
   /// lines until the next timestamped entry starts.
   /// </summary>
   public static IReadOnlyList<string> ErrorsOnly(IReadOnlyList<string> lines)
   {
      ArgumentNullException.ThrowIfNull(lines);
      var kept = new List<string>();
      bool inError = false;
      foreach(string line in lines)
      {
         if(EntryStart().IsMatch(line))
         {
            inError = line.Contains("[ERROR]", StringComparison.Ordinal);
         }
         if(inError)
         {
            kept.Add(line);
         }
      }
      return kept;
   }



   /// <summary>The text a viewer shows for <paramref name="lines"/>, with friendly empty states.</summary>
   public static string ToDisplayText(IReadOnlyList<string> lines, bool errorsOnly)
      => ToDisplayText(lines, errorsOnly, isTruncated: false);



   /// <summary>Shows when the input byte limit hides older entries or cuts the first visible line.</summary>
   public static string ToDisplayText(IReadOnlyList<string> lines, bool errorsOnly, bool isTruncated)
   {
      ArgumentNullException.ThrowIfNull(lines);
      string text = lines.Count == 0
          ? (errorsOnly ? "No errors logged." : "No log entries yet.")
          : string.Join(Environment.NewLine, lines);
      return isTruncated
          ? "Showing a bounded log tail; older entries and the start of the first line may be omitted." + Environment.NewLine + text
          : text;
   }



   /// <summary>Matches the timestamp that starts every log entry, so continuation lines (exception text) can be told apart.</summary>
   [GeneratedRegex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
   private static partial Regex EntryStart();
}
