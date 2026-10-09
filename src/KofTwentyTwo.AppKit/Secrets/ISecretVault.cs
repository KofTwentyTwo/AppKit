/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

namespace KofTwentyTwo.AppKit.Secrets;

/// <summary>
/// Secure storage for secrets (access tokens, API keys) keyed by a caller-chosen name.
/// Secrets must never reach settings files or the activity log; implementations keep
/// them in a platform secret store (<see cref="CredentialManagerVault"/>) or in
/// process memory (<see cref="InMemorySecretVault"/>).
/// </summary>
public interface ISecretVault
{
   /// <summary>
   /// Stores the secret under <paramref name="key"/>, overwriting any existing entry.
   /// Throws on failure: silently losing a secret would leave the user stuck without
   /// any warning.
   /// </summary>
   void Store(string key, string secret);



   /// <summary>The stored secret, or null when the vault has no entry for the key.</summary>
   string? TryRetrieve(string key);



   /// <summary>Removes the key's secret. Deleting an absent entry is a no-op.</summary>
   void Delete(string key);
}
