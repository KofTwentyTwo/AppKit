using System.ComponentModel;
using KofTwentyTwo.AppKit.Secrets;


namespace KofTwentyTwo.AppKit.Tests;

public class InMemorySecretVaultTests
{
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



   [Fact]
   public void ValidatesArguments()
   {
      var vault = new InMemorySecretVault();
      Assert.Throws<ArgumentException>(() => vault.Store("", "v"));
      Assert.Throws<ArgumentNullException>(() => vault.Store("k", null!));
      Assert.Throws<ArgumentNullException>(() => vault.TryRetrieve(null!));
      Assert.Throws<ArgumentException>(() => vault.Delete(""));
   }
}



/// <summary>Runs against the real per-user Credential Manager under a unique prefix.</summary>
public sealed class CredentialManagerVaultTests : IDisposable
{
   private readonly CredentialManagerVault _vault = new("appkit-test-" + Guid.NewGuid().ToString("N"));



   public void Dispose()
   {
      _vault.Delete("token");
      _vault.Delete("empty");
   }



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



   [Fact]
   public void EmptySecret_RoundTrips()
   {
      _vault.Store("empty", "");
      Assert.Equal("", _vault.TryRetrieve("empty"));
   }



   [Fact]
   public void TargetName_IsPrefixColonKey()
   {
      var vault = new CredentialManagerVault("gclo");
      Assert.Equal("gclo:account:abc", vault.TargetName("account:abc"));
      Assert.Throws<ArgumentException>(() => vault.TargetName(""));
   }



   [Fact]
   public void OversizedSecret_FailsLoudly()
   {
      // The Credential Manager caps blobs at 2560 bytes; silently losing a secret is worse than throwing.
      Win32Exception ex = Assert.Throws<Win32Exception>(() => _vault.Store("token", new string('x', 5000)));
      Assert.Contains("CredWriteW", ex.Message, StringComparison.Ordinal);
      Assert.Throws<ArgumentNullException>(() => _vault.Store("token", null!));
   }



   [Fact]
   public void InvalidTargets_SurfaceNativeErrors()
   {
      Assert.Throws<Win32Exception>(() => CredentialManagerVault.TryRetrieveTarget("", "k"));
      Assert.Throws<Win32Exception>(() => CredentialManagerVault.DeleteTarget("", "k"));
   }



   [Fact]
   public void Constructor_ValidatesPlatformAndPrefix()
   {
      Assert.Throws<PlatformNotSupportedException>(() => new CredentialManagerVault("x", isWindows: false));
      Assert.Throws<ArgumentException>(() => new CredentialManagerVault(" "));
   }
}
