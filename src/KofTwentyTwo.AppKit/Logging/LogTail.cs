using System.Text.RegularExpressions;


namespace KofTwentyTwo.AppKit.Logging;

/// <summary>The last lines of a log file and the file length they were read at.</summary>
/// <param name="Lines">The tail, oldest first.</param>
/// <param name="Length">File length in bytes when read; 0 when the file is absent.</param>
public sealed record LogSnapshot(IReadOnlyList<string> Lines, long Length);



/// <summary>
/// Reads and filters <see cref="FileActivityLog"/> files for the log viewer windows.
/// Never throws: this is diagnostics UI, and an unreadable file becomes a readable
/// message instead.
/// </summary>
public static partial class LogTail
{
   /// <summary>Default number of trailing lines a viewer shows.</summary>
   public const int DefaultMaxLines = 2000;



   /// <summary>
   /// The last <paramref name="maxLines"/> lines of <paramref name="path"/>, opened
   /// shared so the logger can keep appending while it is read.
   /// </summary>
   public static LogSnapshot Read(string? path, int maxLines = DefaultMaxLines)
   {
      ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLines);
      try
      {
         if(string.IsNullOrWhiteSpace(path) || !File.Exists(path))
         {
            return new LogSnapshot([], 0);
         }

         using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
         long length = stream.Length;
         using var reader = new StreamReader(stream);

         var tail = new Queue<string>(maxLines);
         while(reader.ReadLine() is string line)
         {
            if(tail.Count == maxLines)
            {
               tail.Dequeue();
            }
            tail.Enqueue(line);
         }
         return new LogSnapshot([.. tail], length);
      }
      catch(Exception ex)
      {
         return new LogSnapshot(["Could not read the log file.", ex.Message], 0);
      }
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
   {
      ArgumentNullException.ThrowIfNull(lines);
      return lines.Count == 0
          ? (errorsOnly ? "No errors logged." : "No log entries yet.")
          : string.Join(Environment.NewLine, lines);
   }



   [GeneratedRegex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}")]
   private static partial Regex EntryStart();
}
