#Requires -Version 7.4
# Copyright (c) 2026 James Maes (KofTwentyTwo)
# SPDX-License-Identifier: MIT
<#
.SYNOPSIS
   Signs only catalogued owned binaries with Microsoft Artifact Signing and verifies them.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
   [Parameter(Mandatory)] [string] $ConfigurationPath,
   [Parameter(Mandatory)] [string] $ManifestPath,
   [Parameter(Mandatory)] [string] $DlibPath,
   [string] $SignToolPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'WindowsSigning.psm1') -Force

if(-not $SignToolPath) { $SignToolPath = Get-WindowsSignTool }
if(-not (Test-Path -LiteralPath $DlibPath -PathType Leaf)) { throw 'Supply the installed x64 Azure.CodeSigning.Dlib.dll path.' }
$configuration = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json
if([string]::IsNullOrWhiteSpace($configuration.PublisherSubject)) { throw 'The verified publisher subject is required.' }
$metadataPath = Join-Path ([IO.Path]::GetTempPath()) ('k22-signing-' + [guid]::NewGuid().ToString('N') + '.json')
try
{
   @{
      Endpoint               = $configuration.Endpoint
      CodeSigningAccountName = $configuration.CodeSigningAccountName
      CertificateProfileName = $configuration.CertificateProfileName
      ExcludeCredentials     = @('EnvironmentCredential', 'WorkloadIdentityCredential', 'ManagedIdentityCredential', 'SharedTokenCacheCredential', 'VisualStudioCredential', 'VisualStudioCodeCredential', 'AzurePowerShellCredential', 'AzureDeveloperCliCredential', 'InteractiveBrowserCredential')
   } | ConvertTo-Json | Set-Content -LiteralPath $metadataPath -Encoding utf8NoBOM
   $entries = @(Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json | Where-Object owned)
   if($entries.Count -eq 0) { throw 'No owned binaries were catalogued.' }
   foreach($entry in $entries)
   {
      $signature = Get-AuthenticodeSignature -LiteralPath $entry.path
      if($signature.Status -eq 'Valid')
      {
         # Preserve existing signatures. A different publisher fails verification below.
         continue
      }
      if($signature.Status -ne 'NotSigned') { throw "Refusing to overwrite an invalid existing signature: $($entry.path)." }
      if($PSCmdlet.ShouldProcess($entry.path, 'Sign with Azure Artifact Signing and RFC3161 timestamp'))
      {
         & $SignToolPath sign /v /fd SHA256 /tr http://timestamp.acs.microsoft.com /td SHA256 /dlib $DlibPath /dmdf $metadataPath $entry.path
         if($LASTEXITCODE -ne 0) { throw "Artifact Signing failed for $($entry.path)." }
      }
   }
   if(-not $WhatIfPreference)
   {
      Assert-WindowsSigningManifest -ManifestPath $ManifestPath -ExpectedSubject $configuration.PublisherSubject -SignTool $SignToolPath
   }
}
finally
{
   if(Test-Path -LiteralPath $metadataPath) { Remove-Item -LiteralPath $metadataPath -Force }
}
