/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;


namespace KofTwentyTwo.AppKit.Secrets;

/// <summary>
/// An <see cref="ISecretVault"/> backed by the Windows Credential Manager: each secret
/// is a generic credential in the per-user store (persisted across reboots, protected
/// by DPAPI) whose blob is the secret's UTF-16 bytes, under the target name
/// "&lt;prefix&gt;:&lt;key&gt;". Talks to advapi32 directly, so it works packaged or
/// unpackaged. Construction on a non-Windows OS throws
/// <see cref="PlatformNotSupportedException"/>.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class CredentialManagerVault : ISecretVault
{
   private const uint CredTypeGeneric = 1;         // CRED_TYPE_GENERIC
   private const uint CredPersistLocalMachine = 2; // CRED_PERSIST_LOCAL_MACHINE: per-user, survives reboot
   private const int ErrorNotFound = 1168;         // ERROR_NOT_FOUND

   private readonly string _prefix;



   /// <summary>Creates a vault whose entries are named "&lt;targetPrefix&gt;:&lt;key&gt;".</summary>
   /// <param name="targetPrefix">Normally the app id, so each app's entries are grouped and distinct.</param>
   public CredentialManagerVault(string targetPrefix)
       : this(targetPrefix, OperatingSystem.IsWindows())
   {
   }



   /// <summary>Test seam: the platform check is a parameter so both arms are coverable.</summary>
   internal CredentialManagerVault(string targetPrefix, bool isWindows)
   {
      ArgumentException.ThrowIfNullOrWhiteSpace(targetPrefix);
      if(!isWindows)
      {
         throw new PlatformNotSupportedException("Secret storage requires the Windows Credential Manager.");
      }
      _prefix = targetPrefix;
   }



   /// <summary>The Credential Manager target name used for <paramref name="key"/>.</summary>
   public string TargetName(string key)
   {
      ArgumentException.ThrowIfNullOrEmpty(key);
      return $"{_prefix}:{key}";
   }



   /// <inheritdoc/>
   public void Store(string key, string secret)
   {
      ArgumentNullException.ThrowIfNull(secret);
      string target = TargetName(key);

      byte[] blob = Encoding.Unicode.GetBytes(secret);
      var pinned = GCHandle.Alloc(blob, GCHandleType.Pinned);
      try
      {
         var credential = new CredentialW
         {
            Type = CredTypeGeneric,
            TargetName = target,
            CredentialBlobSize = (uint)blob.Length,
            CredentialBlob = pinned.AddrOfPinnedObject(),
            Persist = CredPersistLocalMachine,
            UserName = _prefix,
         };

         if(!CredWriteW(ref credential, 0))
         {
            int error = Marshal.GetLastWin32Error();
            throw new Win32Exception(error, $"CredWriteW failed (error {error}) storing secret '{key}'.");
         }
      }
      finally
      {
         // The array is pinned, so the GC cannot have copied it elsewhere: clearing
         // it removes the one deterministic plaintext copy of the secret from the
         // heap (crash dumps, pagefile) before unpinning.
         Array.Clear(blob);
         pinned.Free();
      }
   }



   /// <inheritdoc/>
   public string? TryRetrieve(string key) => TryRetrieveTarget(TargetName(key), key);



   /// <summary>Test seam: an invalid target name makes the non-not-found error arm coverable.</summary>
   internal static string? TryRetrieveTarget(string targetName, string key)
   {
      if(!CredReadW(targetName, CredTypeGeneric, 0, out IntPtr credentialPtr))
      {
         int error = Marshal.GetLastWin32Error();
         if(error == ErrorNotFound)
         {
            return null;
         }
         throw new Win32Exception(error, $"CredReadW failed (error {error}) reading secret '{key}'.");
      }

      try
      {
         CredentialW credential = Marshal.PtrToStructure<CredentialW>(credentialPtr);
         return credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0
             ? ""
             : Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2);
      }
      finally
      {
         CredFree(credentialPtr);
      }
   }



   /// <inheritdoc/>
   public void Delete(string key) => DeleteTarget(TargetName(key), key);



   /// <summary>Test seam: an invalid target name makes the non-not-found error arm coverable.</summary>
   internal static void DeleteTarget(string targetName, string key)
   {
      if(!CredDeleteW(targetName, CredTypeGeneric, 0))
      {
         int error = Marshal.GetLastWin32Error();
         if(error != ErrorNotFound)
         {
            throw new Win32Exception(error, $"CredDeleteW failed (error {error}) deleting secret '{key}'.");
         }
      }
   }



   /// <summary>
   /// Managed mirror of the native CREDENTIALW structure. The explicit StructLayout
   /// both fixes the field order for marshaling and tells the compiler the unread
   /// fields (filled by CredReadW) are intentional.
   /// </summary>
   [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
   private struct CredentialW
   {
      public uint Flags;
      public uint Type;
      [MarshalAs(UnmanagedType.LPWStr)] public string? TargetName;
      [MarshalAs(UnmanagedType.LPWStr)] public string? Comment;
      public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
      public uint CredentialBlobSize;
      public IntPtr CredentialBlob;
      public uint Persist;
      public uint AttributeCount;
      public IntPtr Attributes;
      [MarshalAs(UnmanagedType.LPWStr)] public string? TargetAlias;
      [MarshalAs(UnmanagedType.LPWStr)] public string? UserName;
   }



   /// <summary>advapi32 CredWriteW: creates or replaces a credential.</summary>
   [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
   [return: MarshalAs(UnmanagedType.Bool)]
   private static extern bool CredWriteW(ref CredentialW credential, uint flags);



   /// <summary>advapi32 CredReadW: reads a credential into a buffer that must be released with CredFree.</summary>
   [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
   [return: MarshalAs(UnmanagedType.Bool)]
   private static extern bool CredReadW(string targetName, uint type, uint flags, out IntPtr credential);



   /// <summary>advapi32 CredDeleteW: deletes a credential.</summary>
   [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
   [return: MarshalAs(UnmanagedType.Bool)]
   private static extern bool CredDeleteW(string targetName, uint type, uint flags);



   /// <summary>advapi32 CredFree: releases a buffer returned by CredReadW.</summary>
   [DllImport("advapi32.dll", ExactSpelling = true)]
   private static extern void CredFree(IntPtr buffer);
}
