/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.Collections.Concurrent;


namespace KofTwentyTwo.AppKit.Secrets;

/// <summary>
/// Thread-safe in-memory <see cref="ISecretVault"/>. Used by tests, and for secrets
/// that must not outlive the process.
/// </summary>
public sealed class InMemorySecretVault : ISecretVault
{
   private readonly ConcurrentDictionary<string, string> _secrets = new(StringComparer.Ordinal);



   /// <inheritdoc/>
   public void Store(string key, string secret)
   {
      ArgumentException.ThrowIfNullOrEmpty(key);
      ArgumentNullException.ThrowIfNull(secret);
      _secrets[key] = secret;
   }



   /// <inheritdoc/>
   public string? TryRetrieve(string key)
   {
      ArgumentException.ThrowIfNullOrEmpty(key);
      return _secrets.TryGetValue(key, out string? secret) ? secret : null;
   }



   /// <inheritdoc/>
   public void Delete(string key)
   {
      ArgumentException.ThrowIfNullOrEmpty(key);
      _secrets.TryRemove(key, out _);
   }
}
