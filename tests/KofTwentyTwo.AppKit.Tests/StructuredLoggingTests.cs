/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json;
using KofTwentyTwo.AppKit.Logging;
using Microsoft.Extensions.Logging;


namespace KofTwentyTwo.AppKit.Tests;

/// <summary>Verifies standard templates, typed fields, scopes, failure containment and file sink behavior.</summary>
public sealed class StructuredLoggingTests : IDisposable
{
   private readonly TempDirectory _temp = new();



   /// <summary>Deletes the temporary log folder.</summary>
   public void Dispose() => _temp.Dispose();



   /// <summary>Standard .NET templating preserves the original format and typed values independently of rendering.</summary>
   [Fact]
   public void Log_StandardTemplate_PreservesFieldsAndEventIdentity()
   {
      var sink = new RecordingLog();
      ILogger logger = sink.AsLogger();
      Action<ILogger, int, string, Exception?> logEvent = LoggerMessage.Define<int, string>(
          LogLevel.Information, new EventId(42, "Processed"), "Processed {Count:N0} for {Target}.");
      logEvent(logger, 1234, "sample", null);

      JsonElement record = ReadRecord(sink.Entries.Single());
      Assert.Equal("Processed {Count:N0} for {Target}.", record.GetProperty("MessageTemplate").GetString());
      Assert.Equal("Processed 1,234 for sample.", record.GetProperty("Message").GetString());
      Assert.Equal("Information", record.GetProperty("Level").GetString());
      Assert.Equal(42, record.GetProperty("EventId").GetInt32());
      Assert.Equal("Processed", record.GetProperty("EventName").GetString());
      Assert.Equal(1234, record.GetProperty("Properties").GetProperty("Count").GetInt32());
      Assert.Equal("sample", record.GetProperty("Properties").GetProperty("Target").GetString());
      Assert.False(record.GetProperty("Properties").TryGetProperty("{OriginalFormat}", out _));
   }



   /// <summary>JSON scalars keep their types; complex values have an explicit invariant string fallback.</summary>
   [Fact]
   public void Log_ScalarFields_PreservesTypesAndEscapesText()
   {
      var sink = new RecordingLog();
      using var nested = JsonDocument.Parse("{\"nested\":true}");
      KeyValuePair<string, object?>[] fields =
      [
          new("{OriginalFormat}", "Scalar event"), new("Null", null), new("Boolean", true),
          new("Byte", (byte)255), new("SByte", (sbyte)-128), new("Short", (short)-32768),
          new("UShort", ushort.MaxValue), new("Int", int.MinValue), new("UInt", uint.MaxValue),
          new("Long", long.MinValue), new("ULong", ulong.MaxValue), new("Decimal", 123.45m),
          new("Float", 1.5f), new("Double", 2.5d), new("NaN", double.NaN),
          new("Infinity", double.PositiveInfinity), new("Text", "quoted \"text\"\nnext line"),
          new("Guid", Guid.Empty), new("Nested", nested.RootElement),
      ];
      sink.AsLogger().Log(LogLevel.Debug, default, fields, null, static (_, _) => "Scalar event");

      JsonElement record = ReadRecord(sink.Entries.Single());
      JsonElement values = record.GetProperty("Properties");
      Assert.Equal(JsonValueKind.Null, values.GetProperty("Null").ValueKind);
      Assert.True(values.GetProperty("Boolean").GetBoolean());
      foreach(string key in new[] { "Byte", "SByte", "Short", "UShort", "Int", "UInt", "Long", "ULong", "Decimal", "Float", "Double" })
      {
         Assert.Equal(JsonValueKind.Number, values.GetProperty(key).ValueKind);
      }
      Assert.Equal(long.MinValue, values.GetProperty("Long").GetInt64());
      Assert.Equal(ulong.MaxValue, values.GetProperty("ULong").GetUInt64());
      Assert.Equal(123.45m, values.GetProperty("Decimal").GetDecimal());
      Assert.Equal(1.5d, values.GetProperty("Float").GetDouble());
      Assert.Equal(2.5d, values.GetProperty("Double").GetDouble());
      Assert.Equal("NaN", values.GetProperty("NaN").GetString());
      Assert.Equal("Infinity", values.GetProperty("Infinity").GetString());
      Assert.Equal("quoted \"text\"\nnext line", values.GetProperty("Text").GetString());
      Assert.Equal(Guid.Empty.ToString(), values.GetProperty("Guid").GetString());
      Assert.True(values.GetProperty("Nested").GetProperty("nested").GetBoolean());
      Assert.DoesNotContain('\n', sink.Entries.Single());
      Assert.Equal(JsonValueKind.Null, record.GetProperty("EventName").ValueKind);
   }



   /// <summary>All enabled severities retain the original exception text, even those without a legacy exception overload.</summary>
   [Theory]
   [InlineData(LogLevel.Trace, "INFO")]
   [InlineData(LogLevel.Debug, "INFO")]
   [InlineData(LogLevel.Information, "INFO")]
   [InlineData(LogLevel.Warning, "WARN")]
   [InlineData(LogLevel.Error, "ERROR")]
   [InlineData(LogLevel.Critical, "ERROR")]
   public void Log_EachSeverity_RetainsFullException(LogLevel level, string prefix)
   {
      var sink = new FileActivityLog(_temp.Path, "app");
      var exception = new InvalidOperationException("outer detail", new IOException("inner detail"));
      LoggerMessage.Define<int>(level, new EventId(7), "Failure {Code}")(sink.AsLogger(), 17, exception);

      string text = File.ReadAllText(sink.CurrentLogFilePath);
      Assert.Contains("[" + prefix + "] {", text, StringComparison.Ordinal);
      Assert.Contains(exception.ToString(), text, StringComparison.Ordinal);
      JsonElement record = ReadRecord(File.ReadLines(sink.CurrentLogFilePath).First());
      Assert.Equal(level.ToString(), record.GetProperty("Level").GetString());
      Assert.Equal(17, record.GetProperty("Properties").GetProperty("Code").GetInt32());
      if(level is LogLevel.Error or LogLevel.Critical)
      {
         Assert.Contains(exception.ToString(), LogTail.ToDisplayText(LogTail.ErrorsOnly(LogTail.Read(sink.CurrentLogFilePath).Lines), true), StringComparison.Ordinal);
      }
   }



   /// <summary>Unstructured states remain readable; nested structured and string scopes are disposed independently.</summary>
   [Fact]
   public void Log_NestedScopes_RetainsFieldsAndDisposalOrder()
   {
      var sink = new RecordingLog();
      ILogger logger = sink.AsLogger();
      using(logger.BeginScope("outer"))
      {
         using(logger.BeginScope(new Dictionary<string, object?>(StringComparer.Ordinal) { ["OperationId"] = 7 }))
         {
            logger.Log(LogLevel.Information, default, "plain", null, static (state, _) => state);
         }
         logger.Log(LogLevel.Information, default, "after", null, static (state, _) => state);
      }
      logger.Log(LogLevel.Information, default, "outside", null, static (state, _) => state);

      JsonElement first = ReadRecord(sink.Entries[0]);
      Assert.Equal("plain", first.GetProperty("MessageTemplate").GetString());
      Assert.Empty(first.GetProperty("Properties").EnumerateObject());
      Assert.Equal("outer", first.GetProperty("Scopes")[0].GetString());
      Assert.Equal(7, first.GetProperty("Scopes")[1].GetProperty("OperationId").GetInt32());
      Assert.Single(ReadRecord(sink.Entries[1]).GetProperty("Scopes").EnumerateArray());
      Assert.Empty(ReadRecord(sink.Entries[2]).GetProperty("Scopes").EnumerateArray());
   }



   /// <summary>Concurrent asynchronous scopes cannot mix operation fields or interleave daily file records.</summary>
   [Fact]
   public async Task Log_ConcurrentAsyncScopes_KeepsWholeRecordsAndIsolatesFields()
   {
      var sink = new FileActivityLog(_temp.Path, "parallel");
      ILogger logger = sink.AsLogger();
      Action<ILogger, int, Exception?> logEvent = LoggerMessage.Define<int>(LogLevel.Information, new EventId(9), "Operation {OperationId}");
      await Task.WhenAll(Enumerable.Range(0, 100).Select(async id =>
      {
         using(logger.BeginScope(new Dictionary<string, object?>(StringComparer.Ordinal) { ["OperationId"] = id }))
         {
            await Task.Yield();
            logEvent(logger, id, null);
         }
      }));

      string[] lines = File.ReadAllLines(sink.CurrentLogFilePath);
      Assert.Equal(100, lines.Length);
      var seen = new HashSet<int>();
      foreach(string line in lines)
      {
         JsonElement record = ReadRecord(line);
         int id = record.GetProperty("Properties").GetProperty("OperationId").GetInt32();
         Assert.Equal(id, record.GetProperty("Scopes")[0].GetProperty("OperationId").GetInt32());
         Assert.True(seen.Add(id));
      }
   }



   /// <summary>Structured writes use the same daily rollover and retention policy as legacy entries.</summary>
   [Fact]
   public void Log_DailyFile_RetainsRolloverAndPruning()
   {
      var clock = new ManualTimeProvider(new DateTimeOffset(2026, 3, 15, 9, 30, 0, TimeSpan.Zero));
      File.WriteAllText(_temp.File("app-2026-03-12.log"), "stale");
      var sink = new FileActivityLog(_temp.Path, "app", retentionDays: 3, time: clock);
      ILogger logger = sink.AsLogger();
      logger.Log(LogLevel.Information, default, "day one", null, static (state, _) => state);
      Assert.False(File.Exists(_temp.File("app-2026-03-12.log")));
      clock.Now = clock.Now.AddDays(1);
      logger.Log(LogLevel.Information, default, "day two", null, static (state, _) => state);
      Assert.Equal("day one", ReadRecord(File.ReadAllText(_temp.File("app-2026-03-15.log"))).GetProperty("Message").GetString());
      Assert.Equal("day two", ReadRecord(File.ReadAllText(_temp.File("app-2026-03-16.log"))).GetProperty("Message").GetString());
   }



   /// <summary>Disabled levels bypass formatting and never write an event.</summary>
   [Theory]
   [InlineData(LogLevel.None)]
   [InlineData((LogLevel)(-1))]
   [InlineData((LogLevel)7)]
   public void Log_DisabledLevel_DoesNotEvaluateState(LogLevel level)
   {
      var sink = new RecordingLog();
      ILogger logger = sink.AsLogger();
      Assert.False(logger.IsEnabled(level));
      logger.Log(level, default, "state", null, static (_, _) => throw new InvalidOperationException("must not format"));
      Assert.Empty(sink.Entries);
   }



   /// <summary>Formatter errors and custom sink failures cannot escape any supported logging severity.</summary>
   [Fact]
   public void Log_FailingFormatterOrSink_NeverThrows()
   {
      var sink = new RecordingLog();
      sink.AsLogger().Log(LogLevel.Information, default, "state", null, static (_, _) => throw new InvalidOperationException("formatter failed"));
      Assert.Empty(sink.Entries);
      ILogger logger = new ThrowingActivityLog().AsLogger();
      foreach(LogLevel level in new[] { LogLevel.Information, LogLevel.Warning, LogLevel.Error })
      {
         logger.Log(level, default, "state", null, static (state, _) => state);
      }
   }



   /// <summary>Existing null sinks stay disabled; only creating an adapter rejects a missing sink.</summary>
   [Fact]
   public void AsLogger_NullSink_IsDisabledAndNullArgumentIsRejected()
   {
      Assert.False(NullActivityLog.Instance.AsLogger().IsEnabled(LogLevel.Critical));
      Assert.Throws<ArgumentNullException>(() => ActivityLogExtensions.AsLogger(null!));
   }



   /// <summary>Reads the JSON after either the file prefix or a recording sink's severity.</summary>
   private static JsonElement ReadRecord(string entry)
   {
      using var document = JsonDocument.Parse(entry[entry.IndexOf('{', StringComparison.Ordinal)..]);
      return document.RootElement.Clone();
   }
}



/// <summary>A legacy sink that deliberately violates its contract to exercise the adapter's containment.</summary>
internal sealed class ThrowingActivityLog : IActivityLog
{
   public string LogDirectory => "";

   public string CurrentLogFilePath => "";



   /// <inheritdoc/>
   public void Info(string message) => throw new IOException("sink failed");



   /// <inheritdoc/>
   public void Warning(string message) => throw new IOException("sink failed");



   /// <inheritdoc/>
   public void Error(string message, Exception? exception = null) => throw new IOException("sink failed");
}
