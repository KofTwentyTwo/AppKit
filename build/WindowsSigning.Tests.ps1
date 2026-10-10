#Requires -Version 7.4
# Copyright (c) 2026 James Maes (KofTwentyTwo)
# SPDX-License-Identifier: MIT

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

BeforeDiscovery `
{
   Import-Module (Join-Path $PSScriptRoot 'WindowsSigning.psm1') -Force -Global
}

Describe 'Owned binary catalog' `
{
   BeforeEach `
   {
      $script:Root = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
      New-Item -ItemType Directory -Path $script:Root | Out-Null
      $script:Catalog = Join-Path $script:Root 'catalog.txt'
      $script:Manifest = Join-Path $script:Root 'manifest.json'
      Set-Content -LiteralPath (Join-Path $script:Root 'Owned.dll') -Value 'owned'
      Set-Content -LiteralPath (Join-Path $script:Root 'Microsoft.dll') -Value 'third-party'
   }

   It 'includes only explicit owned names and inventories other binaries' `
   {
      New-WindowsSigningCatalog -Root $script:Root -Patterns @('Owned.dll') -CatalogPath $script:Catalog -ManifestPath $script:Manifest -PreserveOtherBinaries
      @(Get-Content -LiteralPath $script:Catalog) | Should -HaveCount 1
      Get-Content -LiteralPath $script:Catalog | Should -Be 'Owned.dll'
      $entries = @(Get-Content -LiteralPath $script:Manifest -Raw | ConvertFrom-Json)
      $entries | Should -HaveCount 2
      @($entries | Where-Object owned).relativePath | Should -Be 'Owned.dll'
      @($entries | Where-Object { -not $_.owned }).relativePath | Should -Be 'Microsoft.dll'
   }

   It 'rejects a missing required binary rather than silently skipping signing' `
   {
      { New-WindowsSigningCatalog -Root $script:Root -Patterns @('Missing.exe') -CatalogPath $script:Catalog -ManifestPath $script:Manifest } |
         Should -Throw '*No binary matches*'
   }

   It 'rejects parent traversal' `
   {
      { New-WindowsSigningCatalog -Root $script:Root -Patterns @('../Owned.dll') -CatalogPath $script:Catalog -ManifestPath $script:Manifest } |
         Should -Throw '*relative paths*'
   }

   It 'supports WhatIf without writing signing inputs' `
   {
      New-WindowsSigningCatalog -Root $script:Root -Patterns @('Owned.dll') -CatalogPath $script:Catalog -ManifestPath $script:Manifest -WhatIf
      Test-Path -LiteralPath $script:Catalog | Should -BeFalse
      Test-Path -LiteralPath $script:Manifest | Should -BeFalse
   }
}

Describe 'Signature and distributed-byte gates' `
{
   InModuleScope WindowsSigning `
   {
      BeforeAll `
      {
         <#
         .SYNOPSIS
            Supplies deterministic SignTool results for rejection-path tests.
         #>
         function Invoke-ProbeSignTool
         {
            $global:LASTEXITCODE = $script:ProbeExitCode
            return $script:ProbeOutput
         }
      }

      BeforeEach `
      {
         $script:ProbeExitCode = 0
         $script:ProbeOutput = 'Hash of file (sha256): 0123456789'
         $script:ProbeSignature = [pscustomobject]@{
            Status                 = 'Valid'
            SignerCertificate      = [pscustomobject]@{ Subject = 'CN=Verified Publisher'; SubjectName = [pscustomobject]@{ Name = 'CN=Verified Publisher' } }
            TimeStamperCertificate = [pscustomobject]@{ Subject = 'CN=Timestamp Authority' }
         }
         Mock Get-AuthenticodeSignature { return $script:ProbeSignature }
      }

      It 'accepts a trusted timestamped SHA256 signature for the verified subject' `
      {
         { Assert-WindowsSignature -Files @('Owned.dll') -ExpectedSubject 'CN=Verified Publisher' -SignTool Invoke-ProbeSignTool } | Should -Not -Throw
      }

      It 'rejects a native verification failure' `
      {
         $script:ProbeExitCode = 1
         { Assert-WindowsSignature -Files @('Owned.dll') -ExpectedSubject 'CN=Verified Publisher' -SignTool Invoke-ProbeSignTool } | Should -Throw '*verification failed*'
      }

      It 'rejects a different publisher' `
      {
         { Assert-WindowsSignature -Files @('Owned.dll') -ExpectedSubject 'CN=Other Publisher' -SignTool Invoke-ProbeSignTool } | Should -Throw '*Unexpected publisher*'
      }

      It 'rejects a missing timestamp' `
      {
         $script:ProbeSignature.TimeStamperCertificate = $null
         { Assert-WindowsSignature -Files @('Owned.dll') -ExpectedSubject 'CN=Verified Publisher' -SignTool Invoke-ProbeSignTool } | Should -Throw '*timestamped*'
      }

      It 'rejects an untrusted certificate' `
      {
         $script:ProbeSignature.Status = 'NotTrusted'
         { Assert-WindowsSignature -Files @('Owned.dll') -ExpectedSubject 'CN=Verified Publisher' -SignTool Invoke-ProbeSignTool } | Should -Throw '*timestamped*'
      }

      It 'rejects a non-SHA256 digest' `
      {
         $script:ProbeOutput = 'Hash of file (sha1): 0123456789'
         { Assert-WindowsSignature -Files @('Owned.dll') -ExpectedSubject 'CN=Verified Publisher' -SignTool Invoke-ProbeSignTool } | Should -Throw '*SHA256*'
      }

      It 'rejects a SHA1 signature appended alongside a SHA256 signature' `
      {
         $script:ProbeOutput = "Hash of file (sha256): abc`nHash of file (sha1): def"
         { Assert-WindowsSignature -Files @('Owned.dll') -ExpectedSubject 'CN=Verified Publisher' -SignTool Invoke-ProbeSignTool } | Should -Throw '*SHA256*'
      }

      It 'rejects modified third-party binaries' `
      {
         $root = Join-Path $TestDrive 'preserve'
         New-Item -ItemType Directory -Path $root | Out-Null
         Set-Content -LiteralPath (Join-Path $root 'Owned.dll') -Value 'owned'
         Set-Content -LiteralPath (Join-Path $root 'Microsoft.dll') -Value 'third-party'
         $manifest = Join-Path $root 'manifest.json'
         New-WindowsSigningCatalog -Root $root -Patterns @('Owned.dll') -CatalogPath (Join-Path $root 'catalog.txt') -ManifestPath $manifest -PreserveOtherBinaries
         Set-Content -LiteralPath (Join-Path $root 'Microsoft.dll') -Value 'modified'
         { Assert-WindowsSigningManifest -ManifestPath $manifest -ExpectedSubject 'CN=Verified Publisher' -SignTool Invoke-ProbeSignTool } | Should -Throw '*Third-party binary was modified*'
      }

      It 'refuses to catalog another publisher signature for replacement' `
      {
         $root = Join-Path $TestDrive 'other-publisher'
         New-Item -ItemType Directory -Path $root | Out-Null
         Set-Content -LiteralPath (Join-Path $root 'Owned.dll') -Value 'already signed'
         $script:ProbeSignature.SignerCertificate.SubjectName.Name = 'CN=Other Publisher'
         { New-WindowsSigningCatalog -Root $root -Patterns @('Owned.dll') -CatalogPath (Join-Path $root 'catalog.txt') -ManifestPath (Join-Path $root 'manifest.json') -ExpectedSubject 'CN=Verified Publisher' } | Should -Throw '*another publisher*'
         Test-Path -LiteralPath (Join-Path $root 'catalog.txt') | Should -BeFalse
      }

      It 'rejects changed owned bytes in the final package' `
      {
         $root = Join-Path $TestDrive 'changed'
         $assets = Join-Path $root 'assets'
         $packed = Join-Path $root 'packed'
         New-Item -ItemType Directory -Path $root, $assets, $packed | Out-Null
         Set-Content -LiteralPath (Join-Path $root 'Owned.dll') -Value 'signed bytes'
         $manifest = Join-Path $root 'manifest.json'
         New-WindowsSigningCatalog -Root $root -Patterns @('Owned.dll') -CatalogPath (Join-Path $root 'catalog.txt') -ManifestPath $manifest
         Set-Content -LiteralPath (Join-Path $packed 'Owned.dll') -Value 'modified after signing'
         [IO.Compression.ZipFile]::CreateFromDirectory($packed, (Join-Path $assets 'Owned.1.0.0.nupkg'))
         { Assert-WindowsDistribution -Directory $assets -ManifestPath $manifest -ExpectedSubject 'CN=Verified Publisher' -SignTool Invoke-ProbeSignTool } | Should -Throw '*Distributed binary changed*'
      }

      It 'rejects an archive missing its required owned binary' `
      {
         $root = Join-Path $TestDrive 'missing'
         $assets = Join-Path $root 'assets'
         $packed = Join-Path $root 'packed'
         New-Item -ItemType Directory -Path $root, $assets, $packed | Out-Null
         Set-Content -LiteralPath (Join-Path $root 'Owned.dll') -Value 'signed bytes'
         $manifest = Join-Path $root 'manifest.json'
         New-WindowsSigningCatalog -Root $root -Patterns @('Owned.dll') -CatalogPath (Join-Path $root 'catalog.txt') -ManifestPath $manifest
         Set-Content -LiteralPath (Join-Path $packed 'README.txt') -Value 'no binary'
         [IO.Compression.ZipFile]::CreateFromDirectory($packed, (Join-Path $assets 'Owned.1.0.0.nupkg'))
         { Assert-WindowsDistribution -Directory $assets -ManifestPath $manifest -ExpectedSubject 'CN=Verified Publisher' -SignTool Invoke-ProbeSignTool } | Should -Throw '*Owned binary missing*'
      }

      It 'rejects a missing dependency from <Archive>' -TestCases @(
         @{ Archive = 'Owned-Portable.zip'; Head = 'app' },
         @{ Archive = 'Owned-1.0.0-full.nupkg'; Head = 'app' },
         @{ Archive = 'Owned-cli-win-x64.zip'; Head = 'cli' }
      ) `
      {
         param($Archive, $Head)
         $root = Join-Path $TestDrive ('missing-dependency-' + [guid]::NewGuid().ToString('N'))
         $publish = Join-Path $root 'publish'
         $assets = Join-Path $root 'assets'
         $packed = Join-Path $root 'packed'
         New-Item -ItemType Directory -Path "$publish/$Head", $assets, $packed | Out-Null
         Set-Content -LiteralPath "$publish/$Head/Owned.dll" -Value 'signed app'
         Set-Content -LiteralPath "$publish/$Head/Microsoft.dll" -Value 'third-party'
         $manifest = Join-Path $root 'manifest.json'
         New-WindowsSigningCatalog -Root $publish -Patterns @("$Head/Owned.dll") -CatalogPath (Join-Path $root 'catalog.txt') -ManifestPath $manifest -PreserveOtherBinaries
         Copy-Item -LiteralPath "$publish/$Head/Owned.dll" -Destination $packed
         [IO.Compression.ZipFile]::CreateFromDirectory($packed, (Join-Path $assets $Archive))
         { Assert-WindowsDistribution -Directory $assets -ManifestPath $manifest -ExpectedSubject 'CN=Verified Publisher' -SignTool Invoke-ProbeSignTool } | Should -Throw '*Third-party binary missing*'
      }

      It 'allows only the default excluded foreign runtime helpers to be omitted' `
      {
         $root = Join-Path $TestDrive 'excluded-runtime'
         $publish = Join-Path $root 'publish'
         $assets = Join-Path $root 'assets'
         $packed = Join-Path $root 'packed'
         New-Item -ItemType Directory -Path "$publish/app", $assets, $packed | Out-Null
         Set-Content -LiteralPath "$publish/app/Owned.dll" -Value 'signed app'
         Set-Content -LiteralPath "$publish/app/Microsoft.dll" -Value 'third-party'
         Set-Content -LiteralPath "$publish/app/createdump.exe" -Value 'excluded helper'
         Set-Content -LiteralPath "$publish/app/Owned.vshost.exe" -Value 'excluded host'
         $manifest = Join-Path $root 'manifest.json'
         New-WindowsSigningCatalog -Root $publish -Patterns @('app/Owned.dll') -CatalogPath (Join-Path $root 'catalog.txt') -ManifestPath $manifest -PreserveOtherBinaries
         Copy-Item -LiteralPath "$publish/app/Owned.dll", "$publish/app/Microsoft.dll" -Destination $packed
         [IO.Compression.ZipFile]::CreateFromDirectory($packed, (Join-Path $assets 'Owned-Portable.zip'))
         { Assert-WindowsDistribution -Directory $assets -ManifestPath $manifest -ExpectedSubject 'CN=Verified Publisher' -SignTool Invoke-ProbeSignTool } | Should -Not -Throw
      }

      It 'rejects changed dependency bytes in the final archive' `
      {
         $root = Join-Path $TestDrive 'changed-dependency'
         $publish = Join-Path $root 'publish'
         $assets = Join-Path $root 'assets'
         $packed = Join-Path $root 'packed'
         New-Item -ItemType Directory -Path "$publish/app", $assets, $packed | Out-Null
         Set-Content -LiteralPath "$publish/app/Owned.dll" -Value 'signed app'
         Set-Content -LiteralPath "$publish/app/Microsoft.dll" -Value 'third-party'
         $manifest = Join-Path $root 'manifest.json'
         New-WindowsSigningCatalog -Root $publish -Patterns @('app/Owned.dll') -CatalogPath (Join-Path $root 'catalog.txt') -ManifestPath $manifest -PreserveOtherBinaries
         Copy-Item -LiteralPath "$publish/app/Owned.dll" -Destination $packed
         Set-Content -LiteralPath (Join-Path $packed 'Microsoft.dll') -Value 'changed'
         [IO.Compression.ZipFile]::CreateFromDirectory($packed, (Join-Path $assets 'Owned-Portable.zip'))
         { Assert-WindowsDistribution -Directory $assets -ManifestPath $manifest -ExpectedSubject 'CN=Verified Publisher' -SignTool Invoke-ProbeSignTool } | Should -Throw '*Distributed binary changed*'
      }

      It 'accepts the exact signed bytes shipped in a package' `
      {
         $root = Join-Path $TestDrive 'valid'
         $assets = Join-Path $root 'assets'
         New-Item -ItemType Directory -Path $root, $assets | Out-Null
         Set-Content -LiteralPath (Join-Path $root 'Owned.dll') -Value 'signed bytes'
         $manifest = Join-Path $root 'manifest.json'
         New-WindowsSigningCatalog -Root $root -Patterns @('Owned.dll') -CatalogPath (Join-Path $root 'catalog.txt') -ManifestPath $manifest
         $packed = Join-Path $root 'packed'
         New-Item -ItemType Directory -Path $packed | Out-Null
         Copy-Item -LiteralPath (Join-Path $root 'Owned.dll') -Destination $packed
         [IO.Compression.ZipFile]::CreateFromDirectory($packed, (Join-Path $assets 'Owned.1.0.0.nupkg'))
         { Assert-WindowsDistribution -Directory $assets -ManifestPath $manifest -ExpectedSubject 'CN=Verified Publisher' -SignTool Invoke-ProbeSignTool } | Should -Not -Throw
      }

      It 'verifies separate NuGet packages whose IDs share the core package prefix' `
      {
         $root = Join-Path $TestDrive 'prefix'
         $assets = Join-Path $root 'assets'
         New-Item -ItemType Directory -Path $root, $assets | Out-Null
         Set-Content -LiteralPath (Join-Path $root 'Owned.dll') -Value 'signed core'
         Set-Content -LiteralPath (Join-Path $root 'Owned.Adapter.dll') -Value 'signed adapter'
         $manifest = Join-Path $root 'manifest.json'
         New-WindowsSigningCatalog -Root $root -Patterns @('Owned.dll', 'Owned.Adapter.dll') -CatalogPath (Join-Path $root 'catalog.txt') -ManifestPath $manifest
         foreach($name in @('Owned', 'Owned.Adapter'))
         {
            $packed = Join-Path $root $name
            New-Item -ItemType Directory -Path $packed | Out-Null
            Copy-Item -LiteralPath (Join-Path $root "$name.dll") -Destination $packed
            [IO.Compression.ZipFile]::CreateFromDirectory($packed, (Join-Path $assets "$name.1.0.0.nupkg"))
         }
         { Assert-WindowsDistribution -Directory $assets -ManifestPath $manifest -ExpectedSubject 'CN=Verified Publisher' -SignTool Invoke-ProbeSignTool } | Should -Not -Throw
      }

      It 'preserves same-name third-party satellite binaries in different culture directories' `
      {
         $root = Join-Path $TestDrive 'cultures'
         $publish = Join-Path $root 'publish'
         $assets = Join-Path $root 'assets'
         New-Item -ItemType Directory -Path $assets, "$publish/app/en", "$publish/app/fr" | Out-Null
         Set-Content -LiteralPath "$publish/app/Owned.dll" -Value 'signed app'
         Set-Content -LiteralPath "$publish/app/en/Library.resources.dll" -Value 'English'
         Set-Content -LiteralPath "$publish/app/fr/Library.resources.dll" -Value 'French'
         $manifest = Join-Path $root 'manifest.json'
         New-WindowsSigningCatalog -Root $publish -Patterns @('app/Owned.dll') -CatalogPath (Join-Path $root 'catalog.txt') -ManifestPath $manifest -PreserveOtherBinaries
         [IO.Compression.ZipFile]::CreateFromDirectory("$publish/app", (Join-Path $assets 'Owned-Portable.zip'))
         { Assert-WindowsDistribution -Directory $assets -ManifestPath $manifest -ExpectedSubject 'CN=Verified Publisher' -SignTool Invoke-ProbeSignTool } | Should -Not -Throw
      }
   }
}

Describe 'Velopack installer payload' `
{
   BeforeEach `
   {
      $root = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
      New-Item -ItemType Directory -Path $root | Out-Null
      $script:Installer = Join-Path $root 'Probe-Setup.exe'
      $script:FullPackage = Join-Path $root 'Probe-full.nupkg'
      $payload = [Text.Encoding]::UTF8.GetBytes('exact full package payload')
      [IO.File]::WriteAllBytes($script:FullPackage, $payload)
      Copy-Item -LiteralPath (Get-WindowsSignTool) -Destination $script:Installer
      $stream = [IO.File]::Open($script:Installer, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite)
      $reader = [Reflection.PortableExecutable.PEReader]::new($stream, [Reflection.PortableExecutable.PEStreamOptions]::LeaveOpen)
      try
      {
         $headerPosition = $reader.PEHeaders.SectionHeaders[0].PointerToRawData
         $packageOffset = $stream.Length
         $stream.Position = $headerPosition
         $stream.Write([BitConverter]::GetBytes($packageOffset))
         $stream.Write([BitConverter]::GetBytes([long] $payload.Length))
         $stream.Write([Convert]::FromHexString('94F0B17B6893E02937EB34EF53AAE7D42B54F5707EF5D6F57854983E5E94ED7D'))
         $stream.Position = $packageOffset
         $stream.Write($payload)
      }
      finally
      {
         $reader.Dispose()
         $stream.Dispose()
      }
   }

   It 'accepts the exact embedded full-package bytes' `
   {
      { Assert-VelopackInstallerPayload -Installer $script:Installer -FullPackage $script:FullPackage } | Should -Not -Throw
   }

   It 'rejects a different embedded payload of the same length' `
   {
      $bytes = [IO.File]::ReadAllBytes($script:FullPackage)
      $bytes[0] = 0
      [IO.File]::WriteAllBytes($script:FullPackage, $bytes)
      { Assert-VelopackInstallerPayload -Installer $script:Installer -FullPackage $script:FullPackage } | Should -Throw '*different full package*'
   }

   It 'rejects a different full-package length' `
   {
      [IO.File]::WriteAllText($script:FullPackage, 'short')
      { Assert-VelopackInstallerPayload -Installer $script:Installer -FullPackage $script:FullPackage } | Should -Throw '*length differs*'
   }
}
