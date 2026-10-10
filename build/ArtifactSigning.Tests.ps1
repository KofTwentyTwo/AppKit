#Requires -Version 7.4
# Copyright (c) 2026 James Maes (KofTwentyTwo)
# SPDX-License-Identifier: MIT

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

BeforeAll `
{
   $script:IdentityScript = Join-Path $PSScriptRoot 'New-ArtifactSigningIdentity.ps1'
}

Describe 'Azure signing administration boundaries' `
{
   BeforeEach `
   {
      $script:Root = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
      New-Item -ItemType Directory -Path $script:Root | Out-Null
      $script:ConfigurationPath = Join-Path $script:Root 'configuration.json'
      $script:FixturePath = Join-Path $script:Root 'fixture.json'
      $script:LogPath = Join-Path $script:Root 'commands.txt'
      $script:Configuration = @{
         TenantId               = '11111111-1111-1111-1111-111111111111'
         SubscriptionId         = '8ef2479e-3346-4cba-93ad-101c6cd237a0'
         CertificateProfileName = 'existing-profile'
         PublisherSubject       = 'CN=Fixture Publisher'
         ProfileResourceId      = '/subscriptions/8ef2479e-3346-4cba-93ad-101c6cd237a0/resourceGroups/rg-code-signing/providers/Microsoft.CodeSigning/codeSigningAccounts/kof22signing/certificateProfiles/existing-profile'
      }
      $script:Fixture = @{
         TenantId        = $script:Configuration.TenantId
         DefaultTenantId = $script:Configuration.TenantId
         Subject         = $script:Configuration.PublisherSubject
         TrustSubject    = 'repo:KofTwentyTwo/AppKit:environment:release'
      }
      $script:Configuration | ConvertTo-Json | Set-Content -LiteralPath $script:ConfigurationPath
      $script:Fixture | ConvertTo-Json | Set-Content -LiteralPath $script:FixturePath
      $script:Stub = Join-Path $script:Root 'azure-stub.ps1'
      # The external fake accepts the same argv as az; it has no credentials or network.
      @'
param([Parameter(ValueFromRemainingArguments)] [string[]] $Arguments)
$ErrorActionPreference = 'Stop'
$fixture = Get-Content (Join-Path $PSScriptRoot 'fixture.json') -Raw | ConvertFrom-Json
Add-Content -LiteralPath (Join-Path $PSScriptRoot 'commands.txt') -Value ($Arguments -join ' ')
$roleId = '/subscriptions/8ef2479e-3346-4cba-93ad-101c6cd237a0/providers/Microsoft.Authorization/roleDefinitions/22222222-2222-2222-2222-222222222222'
$key = $Arguments[0..1] -join ' '
$result = switch ($key) {
   'account show' { @{tenantId=$(if ($Arguments -contains '--subscription') {$fixture.TenantId} else {$fixture.DefaultTenantId})} }
   'rest --method' { @{properties=@{status='Active';profileType='PublicTrust';certificates=@(@{subjectName=$fixture.Subject})}} }
   'role definition' { @(@{id=$roleId;roleType='BuiltInRole'}) }
   'ad app' {
      if ($Arguments[2] -eq 'show') { @{id='app-object';appId='33333333-3333-3333-3333-333333333333'} }
      elseif ($Arguments[2] -eq 'federated-credential' -and $Arguments[3] -eq 'list') {
         @(@{name='github-appkit-release';subject=$fixture.TrustSubject;issuer='https://token.actions.githubusercontent.com';audiences=@('api://AzureADTokenExchange')},
           @{name='other-repo';subject='repo:KofTwentyTwo/OtherApp:environment:release';issuer='https://token.actions.githubusercontent.com';audiences=@('api://AzureADTokenExchange')})
      }
      else { throw 'Unexpected Entra mutation in fixture.' }
   }
   'ad sp' { @(@{id='principal-object'}) }
   'role assignment' { @(@{roleDefinitionId=$roleId}) }
   default { throw "Unexpected command: $key" }
}
ConvertTo-Json -InputObject $result -Depth 6 -Compress
'@ | Set-Content -LiteralPath $script:Stub
   }

   It 'reuses the CI identity, exact federation and signer assignment without writes' `
   {
      $result = & $script:IdentityScript -ConfigurationPath $script:ConfigurationPath -ExistingClientId '33333333-3333-3333-3333-333333333333' -AzureCli pwsh -AzureCliArguments @('-NoProfile', '-File', $script:Stub)
      ($result | ConvertFrom-Json).FederatedSubject | Should -Be 'repo:KofTwentyTwo/AppKit:environment:release'
      (Get-Content -LiteralPath $script:LogPath -Raw) | Should -Not -Match '\b(create|delete|update)\b'
   }

   It 'rejects a different active tenant before touching Entra identities' `
   {
      $script:Fixture.TenantId = '44444444-4444-4444-4444-444444444444'
      $script:Fixture | ConvertTo-Json | Set-Content -LiteralPath $script:FixturePath
      { & $script:IdentityScript -ConfigurationPath $script:ConfigurationPath -ExistingClientId '33333333-3333-3333-3333-333333333333' -AzureCli pwsh -AzureCliArguments @('-NoProfile', '-File', $script:Stub) } | Should -Throw '*tenant differs*'
      (Get-Content -LiteralPath $script:LogPath -Raw) | Should -Not -Match '\bad app\b'
   }

   It 'rejects account-wide scope before granting any role' `
   {
      $script:Configuration.ProfileResourceId = '/subscriptions/8ef2479e-3346-4cba-93ad-101c6cd237a0/resourceGroups/rg-code-signing/providers/Microsoft.CodeSigning/codeSigningAccounts/kof22signing'
      $script:Configuration | ConvertTo-Json | Set-Content -LiteralPath $script:ConfigurationPath
      { & $script:IdentityScript -ConfigurationPath $script:ConfigurationPath -AzureCli pwsh -AzureCliArguments @('-NoProfile', '-File', $script:Stub) } | Should -Throw '*outside the verified existing certificate profile*'
      (Get-Content -LiteralPath $script:LogPath -Raw) | Should -Not -Match '\brole assignment\b'
   }

   It 'rejects another tenant as the default Graph context without switching it' `
   {
      $script:Fixture.DefaultTenantId = '44444444-4444-4444-4444-444444444444'
      $script:Fixture | ConvertTo-Json | Set-Content -LiteralPath $script:FixturePath
      { & $script:IdentityScript -ConfigurationPath $script:ConfigurationPath -AzureCli pwsh -AzureCliArguments @('-NoProfile', '-File', $script:Stub) } | Should -Throw '*active CLI tenant differs*'
      (Get-Content -LiteralPath $script:LogPath -Raw) | Should -Not -Match '\b(ad app|account set)\b'
   }

   It 'rejects a stale publisher subject before creating credentials' `
   {
      $script:Fixture.Subject = 'CN=Different Publisher'
      $script:Fixture | ConvertTo-Json | Set-Content -LiteralPath $script:FixturePath
      { & $script:IdentityScript -ConfigurationPath $script:ConfigurationPath -AzureCli pwsh -AzureCliArguments @('-NoProfile', '-File', $script:Stub) } | Should -Throw '*no longer matches*'
      (Get-Content -LiteralPath $script:LogPath -Raw) | Should -Not -Match '\bad app\b'
   }

   It 'preserves an existing credential whose name has different trust' `
   {
      $script:Fixture.TrustSubject = 'repo:KofTwentyTwo/OtherApp:environment:release'
      $script:Fixture | ConvertTo-Json | Set-Content -LiteralPath $script:FixturePath
      { & $script:IdentityScript -ConfigurationPath $script:ConfigurationPath -ExistingClientId '33333333-3333-3333-3333-333333333333' -AzureCli pwsh -AzureCliArguments @('-NoProfile', '-File', $script:Stub) } | Should -Throw '*different trust*'
      (Get-Content -LiteralPath $script:LogPath -Raw) | Should -Not -Match '\b(create|delete|update)\b'
   }
}
