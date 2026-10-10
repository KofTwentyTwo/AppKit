/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;


namespace KofTwentyTwo.AppKit.Logging;

/// <summary>Stores standard logging state without reflection-based serialization or custom template parsing.</summary>
internal sealed class ActivityLogLogger : ILogger
{
   private readonly IActivityLog _log;
   private readonly LoggerExternalScopeProvider _scopes = new();



   /// <summary>Wraps an existing sink; the public extension validates it.</summary>
   public ActivityLogLogger(IActivityLog log) => _log = log;



   /// <inheritdoc/>
   public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _scopes.Push(state);



   /// <inheritdoc/>
   public bool IsEnabled(LogLevel logLevel) => logLevel is >= LogLevel.Trace and < LogLevel.None;



   /// <summary>Formats and writes one event; even malformed state or a failing custom sink cannot escape.</summary>
   public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
   {
      if(!IsEnabled(logLevel))
      {
         return;
      }
      try
      {
         string message = formatter(state, exception);
         KeyValuePair<string, object?>[] fields = state is IEnumerable<KeyValuePair<string, object?>> values ? [.. values] : [];
         string template = fields.LastOrDefault(pair => string.Equals(pair.Key, "{OriginalFormat}", StringComparison.Ordinal)).Value as string ?? message;
         using var buffer = new MemoryStream();
         using(var writer = new Utf8JsonWriter(buffer))
         {
            writer.WriteStartObject();
            writer.WriteString("MessageTemplate", template);
            writer.WriteString("Message", message);
            writer.WriteString("Level", logLevel.ToString());
            writer.WriteNumber("EventId", eventId.Id);
            writer.WriteString("EventName", eventId.Name);
            writer.WritePropertyName("Properties");
            WriteProperties(writer, fields);
            writer.WriteStartArray("Scopes");
            _scopes.ForEachScope(static (scope, json) =>
            {
               if(scope is IEnumerable<KeyValuePair<string, object?>> properties)
               {
                  WriteProperties(json, properties);
               }
               else
               {
                  WriteValue(json, scope);
               }
            }, writer);
            writer.WriteEndArray();
            writer.WriteEndObject();
         }
         string entry = Encoding.UTF8.GetString(buffer.ToArray());
         switch(logLevel)
         {
            case LogLevel.Warning:
               _log.Warning(exception is null ? entry : entry + Environment.NewLine + exception);
               break;
            case LogLevel.Error:
            case LogLevel.Critical:
               _log.Error(entry, exception);
               break;
            default:
               // The legacy sink has three severities; JSON retains the precise level.
               if(exception is not null)
               {
                  entry += Environment.NewLine + exception;
               }
               _log.Info(entry);
               break;
         }
      }
      catch
      {
         // Diagnostics must never stop the operation being diagnosed.
      }
   }



   /// <summary>Writes named fields separately from the original message template.</summary>
   private static void WriteProperties(Utf8JsonWriter writer, IEnumerable<KeyValuePair<string, object?>> properties)
   {
      writer.WriteStartObject();
      foreach(KeyValuePair<string, object?> property in properties)
      {
         if(!string.Equals(property.Key, "{OriginalFormat}", StringComparison.Ordinal))
         {
            writer.WritePropertyName(property.Key);
            WriteValue(writer, property.Value);
         }
      }
      writer.WriteEndObject();
   }



   /// <summary>Preserves scalar JSON types; other values use an invariant string without inspecting object members.</summary>
   private static void WriteValue(Utf8JsonWriter writer, object? value)
   {
      switch(value)
      {
         case null:
            writer.WriteNullValue();
            break;
         case bool boolean:
            writer.WriteBooleanValue(boolean);
            break;
         case byte or sbyte or short or ushort or int or uint or long:
            writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
            break;
         case ulong unsigned:
            writer.WriteNumberValue(unsigned);
            break;
         case decimal number:
            writer.WriteNumberValue(number);
            break;
         case float or double:
            double floating = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            if(double.IsFinite(floating))
            {
               writer.WriteNumberValue(floating);
            }
            else
            {
               writer.WriteStringValue(floating.ToString(CultureInfo.InvariantCulture));
            }
            break;
         case JsonElement element:
            element.WriteTo(writer);
            break;
         default:
            writer.WriteStringValue(Convert.ToString(value, CultureInfo.InvariantCulture));
            break;
      }
   }
}
