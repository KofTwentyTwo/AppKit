/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.ComponentModel;
using KofTwentyTwo.AppKit.Secrets;


namespace KofTwentyTwo.AppKit.Tests;

/// <summary>Tests for the in-memory secret vault.</summary>
public class InMemorySecretVaultTests
{
   /// <summary>Store, overwrite, retrieve, and delete behave like a dictionary, and deleting twice is harmless.</summary>
   [Fact]
   public void StoreRetrieveDelete()
   {
      var vault = new InMemorySecretVault();
      Assert.Null(vault.TryRetrieve("k"));
      vault.Store("k", "v1");
      vault.Store("k", "v2");
      Assert.Equal("v2", vault.TryRetrieve("k"));
      vault.Delete("k");
      vault.Delete("k");
      Assert.Null(vault.TryRetrieve("k"));
   }



   /// <summary>Empty keys and null secrets are rejected.</summary>
   [Fact]
   public void ValidatesArguments()
   {
      var vault = new InMemorySecretVault();
      Assert.Throws<ArgumentException>(() => vault.Store("", "v"));
      Assert.Throws<ArgumentNullException>(() => vault.Store("k", null!));
      Assert.Throws<ArgumentNullException>(() => vault.TryRetrieve(null!));
      Assert.Throws<ArgumentException>(() => vault.TryRetrieve(""));
      Assert.Throws<ArgumentException>(() => vault.Delete(""));
   }
}



/// <summary>Runs against the real per-user Credential Manager under a unique prefix.</summary>
public sealed class CredentialManagerVaultTests : IDisposable
{
   private readonly CredentialManagerVault _vault = new("appkit-test-" + Guid.NewGuid().ToString("N"));



   /// <summary>Removes the credentials the test may have created.</summary>
   public void Dispose()
   {
      _vault.Delete("token");
      _vault.Delete("empty");
   }



   /// <summary>A secret round-trips through the Credential Manager, overwrites in place, and deletes cleanly.</summary>
   [Fact]
   public void StoreRetrieveOverwriteDelete()
   {
      Assert.Null(_vault.TryRetrieve("token"));
      _vault.Store("token", "ghp_first");
      _vault.Store("token", "ghp_second ✓");
      Assert.Equal("ghp_second ✓", _vault.TryRetrieve("token"));
      _vault.Delete("token");
      _vault.Delete("token");
      Assert.Null(_vault.TryRetrieve("token"));
   }



   /// <summary>EmptySecret: round trips.</summary>
   [Fact]
   public void EmptySecret_RoundTrips()
   {
      _vault.Store("empty", "");
      Assert.Equal("", _vault.TryRetrieve("empty"));
   }



   /// <summary>TargetName: is prefix colon key.</summary>
   [Fact]
   public void TargetName_IsPrefixColonKey()
   {
      var vault = new CredentialManagerVault("gclo");
      Assert.Equal("gclo:account:abc", vault.TargetName("account:abc"));
      Assert.Throws<ArgumentException>(() => vault.TargetName(""));
   }



   /// <summary>OversizedSecret: fails loudly.</summary>
   [Fact]
   public void OversizedSecret_FailsLoudly()
   {
      // The Credential Manager caps blobs at 2560 bytes; silently losing a secret is worse than throwing.
      Win32Exception ex = Assert.Throws<Win32Exception>(() => _vault.Store("token", new string('x', 5000)));
      Assert.Contains("CredWriteW", ex.Message, StringComparison.Ordinal);
      Assert.Equal("secret", Assert.Throws<ArgumentNullException>(() => _vault.Store("token", null!)).ParamName);
   }



   /// <summary>InvalidTargets: surface native errors.</summary>
   [Fact]
   public void InvalidTargets_SurfaceNativeErrors()
   {
      Win32Exception read = Assert.Throws<Win32Exception>(() => CredentialManagerVault.TryRetrieveTarget("", "k"));
      Assert.Contains("CredReadW", read.Message, StringComparison.Ordinal);
      Assert.Contains("'k'", read.Message, StringComparison.Ordinal);
      Win32Exception delete = Assert.Throws<Win32Exception>(() => CredentialManagerVault.DeleteTarget("", "k"));
      Assert.Contains("CredDeleteW", delete.Message, StringComparison.Ordinal);
      Assert.Contains("'k'", delete.Message, StringComparison.Ordinal);
   }



   /// <summary>Constructor: validates platform and prefix.</summary>
   [Fact]
   public void Constructor_ValidatesPlatformAndPrefix()
   {
      PlatformNotSupportedException error = Assert.Throws<PlatformNotSupportedException>(() => new CredentialManagerVault("x", isWindows: false));
      Assert.Contains("Windows Credential Manager", error.Message, StringComparison.Ordinal);
      Assert.Throws<ArgumentException>(() => new CredentialManagerVault(" "));
   }
}
