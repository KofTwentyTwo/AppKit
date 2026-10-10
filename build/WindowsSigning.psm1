#Requires -Version 7.4
# Copyright (c) 2026 James Maes (KofTwentyTwo)
# SPDX-License-Identifier: MIT

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

<#
.SYNOPSIS
   Resolves the newest installed x64 Windows SDK SignTool.
#>
function Get-WindowsSignTool
{
   [CmdletBinding()]
   param()

   $sdk = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin'
   $tools = @(Get-ChildItem -Path "$sdk/*/x64/signtool.exe" -File -ErrorAction SilentlyContinue |
         Sort-Object { [version] $_.Directory.Parent.Name } -Descending)
   if($tools.Count -eq 0) { throw 'Install the Windows SDK with x64 SignTool.' }
   return $tools[0].FullName
}



<#
.SYNOPSIS
   Builds an explicit signing catalog and records other binaries for preservation checks.
#>
function New-WindowsSigningCatalog
{
   [CmdletBinding(SupportsShouldProcess)]
   param(
      [Parameter(Mandatory)] [string] $Root,
      [Parameter(Mandatory)] [string[]] $Patterns,
      [Parameter(Mandatory)] [string] $CatalogPath,
      [Parameter(Mandatory)] [string] $ManifestPath,
      [string] $ExpectedSubject = '',
      [switch] $PreserveOtherBinaries
   )

   $rootPath = (Resolve-Path -LiteralPath $Root).Path
   $binaries = @(Get-ChildItem -LiteralPath $rootPath -File -Recurse |
         Where-Object Extension -In '.exe', '.dll', '.msi', '.msix')
   $owned = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
   foreach($pattern in $Patterns)
   {
      $pattern = $pattern.Trim().Replace('\', '/')
      if([string]::IsNullOrWhiteSpace($pattern) -or $pattern.Split('/') -contains '..' -or [IO.Path]::IsPathRooted($pattern))
      {
         throw "Signing patterns must be nonempty relative paths or file names: '$pattern'."
      }
      $catalogMatches = @($binaries | Where-Object {
            $candidate = if($pattern.Contains('/')) { [IO.Path]::GetRelativePath($rootPath, $_.FullName).Replace('\', '/') } else { $_.Name }
            $candidate -like $pattern
         })
      if($catalogMatches.Count -eq 0) { throw "No binary matches required signing pattern '$pattern'." }
      foreach($file in $catalogMatches)
      {
         if($ExpectedSubject)
         {
            $expected = [Security.Cryptography.X509Certificates.X500DistinguishedName]::new($ExpectedSubject).Name
            $existing = Get-AuthenticodeSignature -LiteralPath $file.FullName
            if($null -ne $existing.SignerCertificate -and $existing.SignerCertificate.SubjectName.Name -cne $expected)
            {
               throw "Refusing to replace another publisher's signature on '$($file.FullName)'. Keep it outside the owned catalog."
            }
            if($existing.Status -notin 'Valid', 'NotSigned') { throw "Invalid existing signature on '$($file.FullName)'." }
         }
         $null = $owned.Add($file.FullName)
      }
   }
   $entries = @($binaries | Where-Object { $PreserveOtherBinaries -or $owned.Contains($_.FullName) } | ForEach-Object {
         [ordered]@{
            path         = $_.FullName
            relativePath = [IO.Path]::GetRelativePath($rootPath, $_.FullName).Replace('\', '/')
            owned        = $owned.Contains($_.FullName)
            sha256       = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
         }
      })
   $catalogDirectory = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($CatalogPath))
   if(-not $PSCmdlet.ShouldProcess("$CatalogPath; $ManifestPath", 'Write signing catalog and preservation manifest')) { return }
   New-Item -ItemType Directory -Force -Path $catalogDirectory | Out-Null
   @($owned | Sort-Object | ForEach-Object { [IO.Path]::GetRelativePath($catalogDirectory, $_) }) |
      Set-Content -LiteralPath $CatalogPath -Encoding utf8NoBOM
   ConvertTo-Json -InputObject $entries -Depth 5 | Set-Content -LiteralPath $ManifestPath -Encoding utf8NoBOM
}



<#
.SYNOPSIS
   Requires all Authenticode signatures to validate, with the configured publisher and a timestamp.
#>
function Assert-WindowsSignature
{
   [CmdletBinding()]
   param(
      [Parameter(Mandatory)] [string[]] $Files,
      [Parameter(Mandatory)] [ValidateNotNullOrEmpty()] [string] $ExpectedSubject,
      [string] $SignTool = (Get-WindowsSignTool)
   )

   if($Files.Count -eq 0) { throw 'No shipped binaries were supplied for verification.' }
   $expected = [Security.Cryptography.X509Certificates.X500DistinguishedName]::new($ExpectedSubject).Name
   foreach($file in $Files)
   {
      $output = (& $SignTool verify /pa /all /v $file 2>&1) -join "`n"
      Write-Output $output
      if($LASTEXITCODE -ne 0) { throw "SignTool verification failed for '$file'." }
      $signature = Get-AuthenticodeSignature -LiteralPath $file
      if($signature.Status -ne 'Valid' -or $null -eq $signature.SignerCertificate -or $null -eq $signature.TimeStamperCertificate)
      {
         throw "Missing valid timestamped Authenticode signature on '$file'."
      }
      if($signature.SignerCertificate.SubjectName.Name -cne $expected)
      {
         throw "Unexpected publisher on '$file': $($signature.SignerCertificate.Subject)."
      }
      $digests = @([regex]::Matches($output, '(?i)Hash of file \(([^)]+)\)') | ForEach-Object { $_.Groups[1].Value })
      if($digests.Count -eq 0 -or @($digests | Where-Object { $_ -ne 'sha256' }).Count -gt 0)
      {
         throw "SignTool did not report a SHA256 file digest for '$file'. Use the English Windows SDK verification output."
      }
   }
}



<#
.SYNOPSIS
   Verifies signed catalog entries and checks that other binary bytes were preserved.
#>
function Assert-WindowsSigningManifest
{
   [CmdletBinding()]
   param(
      [Parameter(Mandatory)] [string] $ManifestPath,
      [Parameter(Mandatory)] [string] $ExpectedSubject,
      [string] $SignTool = (Get-WindowsSignTool)
   )

   $entries = @(Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json)
   $owned = @($entries | Where-Object owned | ForEach-Object path)
   Assert-WindowsSignature -Files $owned -ExpectedSubject $ExpectedSubject -SignTool $SignTool
   foreach($entry in $entries | Where-Object { -not $_.owned })
   {
      if((Get-FileHash -LiteralPath $entry.path -Algorithm SHA256).Hash -ne $entry.sha256)
      {
         throw "Third-party binary was modified: $($entry.relativePath)."
      }
   }
}



<#
.SYNOPSIS
   Checks signed binaries inside the final ZIP/NuGet assets and their bytes against the signed build.
#>
function Assert-WindowsDistribution
{
   [CmdletBinding()]
   param(
      [Parameter(Mandatory)] [string] $Directory,
      [Parameter(Mandatory)] [string] $ManifestPath,
      [Parameter(Mandatory)] [string] $ExpectedSubject,
      [string] $SignTool = (Get-WindowsSignTool)
   )

   $entries = @(Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json)
   $owned = @($entries | Where-Object owned)
   if($owned.Count -eq 0) { throw 'The signing manifest has no owned binaries.' }
   $assets = @(Get-ChildItem -LiteralPath $Directory -File)
   $archives = @($assets | Where-Object { $_.Extension -in '.zip', '.nupkg' -and $_.Name -notlike '*-delta.nupkg' })
   if($archives.Count -eq 0) { throw 'No final ZIP or full NuGet package was produced.' }
   $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
   foreach($archive in $archives)
   {
      $scratch = Join-Path ([IO.Path]::GetTempPath()) ('k22-signing-' + [guid]::NewGuid().ToString('N'))
      try
      {
         [IO.Compression.ZipFile]::ExtractToDirectory($archive.FullName, $scratch)
         $files = @(Get-ChildItem -LiteralPath $scratch -File -Recurse)
         $archiveSeen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
         foreach($entry in $entries)
         {
            $name = [IO.Path]::GetFileName($entry.path)
            $packageRelative = if($entry.relativePath.StartsWith('app/') -or $entry.relativePath.StartsWith('cli/')) { $entry.relativePath.Substring(4) } else { $name }
            $copies = @($files | Where-Object {
                  $relative = [IO.Path]::GetRelativePath($scratch, $_.FullName).Replace('\', '/')
                  $_.Name -eq $name -and ($relative -eq $packageRelative -or $relative.EndsWith('/' + $packageRelative, [StringComparison]::OrdinalIgnoreCase))
               })
            foreach($copy in $copies)
            {
               $hash = (Get-FileHash -LiteralPath $copy.FullName -Algorithm SHA256).Hash
               $expectedHash = if($entry.owned) { (Get-FileHash -LiteralPath $entry.path -Algorithm SHA256).Hash } else { $entry.sha256 }
               if($hash -ne $expectedHash)
               {
                  # The portable archive also carries a generated launcher at its root.
                  if($entry.owned -and $copy.Extension -eq '.exe' -and $copy.DirectoryName -eq $scratch -and $archive.Name -like '*-Portable.zip')
                  {
                     Assert-WindowsSignature -Files @($copy.FullName) -ExpectedSubject $ExpectedSubject -SignTool $SignTool
                     continue
                  }
                  throw "Distributed binary changed in $($archive.Name): $name."
               }
               if($entry.owned)
               {
                  Assert-WindowsSignature -Files @($copy.FullName) -ExpectedSubject $ExpectedSubject -SignTool $SignTool
                  $null = $seen.Add($entry.path)
                  $null = $archiveSeen.Add($entry.path)
               }
            }
         }
         $required = @($owned | Where-Object {
               if($archive.Name -like '*-cli-*.zip') { return $_.relativePath.StartsWith('cli/') }
               if($archive.Name -like '*-Portable.zip' -or $archive.Name -like '*-full.nupkg') { return $_.relativePath.StartsWith('app/') }
               $packageId = [regex]::Escape([IO.Path]::GetFileNameWithoutExtension($_.path))
               return $archive.Name -match "^$packageId\.[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?\.nupkg$"
            })
         foreach($entry in $required)
         {
            if(-not $archiveSeen.Contains($entry.path)) { throw "Owned binary missing from $($archive.Name): $($entry.relativePath)." }
         }
         # Velopack creates these branded helpers before its signing hook.
         $updates = @($files | Where-Object { $_.Name -in 'Update.exe', 'Squirrel.exe' -or $_.Name -like '*_ExecutionStub.exe' } | ForEach-Object FullName)
         if($updates.Count -gt 0) { Assert-WindowsSignature -Files $updates -ExpectedSubject $ExpectedSubject -SignTool $SignTool }
      }
      finally
      {
         $temporaryRoot = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath([IO.Path]::GetTempPath()))
         if([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($scratch)) -ne $temporaryRoot -or [IO.Path]::GetFileName($scratch) -notlike 'k22-signing-*')
         {
            throw 'Refusing to remove a directory outside the signing scratch location.'
         }
         if(Test-Path -LiteralPath $scratch) { Remove-Item -LiteralPath $scratch -Recurse -Force }
      }
   }
   foreach($entry in $owned)
   {
      if(-not $seen.Contains($entry.path)) { throw "Owned binary is missing from distributed archives: $($entry.relativePath)." }
   }
   $installers = @($assets | Where-Object Extension -In '.exe', '.msi', '.msix' | ForEach-Object FullName)
   if($installers.Count -gt 0) { Assert-WindowsSignature -Files $installers -ExpectedSubject $ExpectedSubject -SignTool $SignTool }
   foreach($setup in $assets | Where-Object Name -Like '*-Setup.exe')
   {
      $fullPackages = @($assets | Where-Object Name -Like '*-full.nupkg')
      if($fullPackages.Count -ne 1) { throw 'Require one current full package to verify the installer payload.' }
      Assert-VelopackInstallerPayload -Installer $setup.FullName -FullPackage $fullPackages[0].FullName
   }
}



<#
.SYNOPSIS
   Requires the signed Velopack installer to embed the exact verified full update package.
#>
function Assert-VelopackInstallerPayload
{
   [CmdletBinding()]
   param(
      [Parameter(Mandatory)] [string] $Installer,
      [Parameter(Mandatory)] [string] $FullPackage
   )

   # Velopack 1.2.161 SetupBundle: two little-endian Int64s followed by this marker.
   $marker = '94F0B17B6893E02937EB34EF53AAE7D42B54F5707EF5D6F57854983E5E94ED7D'
   $stream = [IO.File]::OpenRead($Installer)
   $reader = [Reflection.PortableExecutable.PEReader]::new($stream, [Reflection.PortableExecutable.PEStreamOptions]::LeaveOpen)
   $hasher = [Security.Cryptography.IncrementalHash]::CreateHash([Security.Cryptography.HashAlgorithmName]::SHA256)
   try
   {
      $offset = 0L
      $length = 0L
      $headersFound = 0
      foreach($section in $reader.PEHeaders.SectionHeaders)
      {
         [byte[]] $bytes = $reader.GetSectionData($section.VirtualAddress).GetContent()
         $position = [Convert]::ToHexString($bytes).IndexOf($marker, [StringComparison]::Ordinal)
         if($position -lt 32 -or $position % 2 -ne 0) { continue }
         $headersFound++
         $offset = [BitConverter]::ToInt64($bytes, $position / 2 - 16)
         $length = [BitConverter]::ToInt64($bytes, $position / 2 - 8)
      }
      if($headersFound -ne 1 -or $offset -le 0 -or $length -le 0 -or $offset -gt $stream.Length -or $length -gt $stream.Length - $offset)
      {
         throw 'The Velopack installer bundle header is missing, ambiguous or outside the file.'
      }
      if($length -ne (Get-Item -LiteralPath $FullPackage).Length) { throw 'Installer payload length differs from the full package.' }
      $stream.Position = $offset
      $buffer = [byte[]]::new(1048576)
      $remaining = $length
      while($remaining -gt 0)
      {
         $count = $stream.Read($buffer, 0, [int] [math]::Min($buffer.Length, $remaining))
         if($count -eq 0) { throw 'Unexpected end of the installer payload.' }
         $hasher.AppendData($buffer, 0, $count)
         $remaining -= $count
      }
      $hash = [Convert]::ToHexString($hasher.GetHashAndReset())
      if($hash -ne (Get-FileHash -LiteralPath $FullPackage -Algorithm SHA256).Hash) { throw 'Installer embeds a different full package.' }
   }
   finally
   {
      $hasher.Dispose()
      $reader.Dispose()
      $stream.Dispose()
   }
}

Export-ModuleMember -Function Get-WindowsSignTool, New-WindowsSigningCatalog, Assert-WindowsSignature, Assert-WindowsSigningManifest, Assert-WindowsDistribution, Assert-VelopackInstallerPayload
