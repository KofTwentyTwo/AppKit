#Requires -Version 7.4
# Copyright (c) 2026 James Maes (KofTwentyTwo)
# SPDX-License-Identifier: MIT
<#
.SYNOPSIS
   Configures repository-specific OIDC trust and profile-scoped signing permission in the verified existing tenant.
.DESCRIPTION
   Preserves other applications, federated credentials and assignments. Never creates signing accounts or profiles.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
   [Parameter(Mandatory)] [string] $ConfigurationPath,
   [ValidatePattern('^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$')] [string] $Repository = 'KofTwentyTwo/AppKit',
   [ValidatePattern('^[A-Za-z0-9_.-]+$')] [string] $Environment = 'release',
   [string] $ExistingClientId = '',
   [string] $AzureCli = 'az',
   [string[]] $AzureCliArguments = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$azureExecutable = $AzureCli
$azureCommandPrefix = $AzureCliArguments
$configuration = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json

<#
.SYNOPSIS
   Executes an Azure administrative command without emitting tokens or credentials.
#>
function Invoke-AzureJson
{
   [CmdletBinding()]
   param([Parameter(Mandatory)] [string[]] $Arguments)

   $json = (& $azureExecutable @azureCommandPrefix @Arguments --output json --only-show-errors) -join "`n"
   if($LASTEXITCODE -ne 0) { throw "Azure command failed: $($Arguments[0..1] -join ' ')." }
   if(-not [string]::IsNullOrWhiteSpace($json)) { return $json | ConvertFrom-Json }
}

$subscription = Invoke-AzureJson -Arguments @('account', 'show', '--subscription', $configuration.SubscriptionId)
if($subscription.tenantId -ne $configuration.TenantId) { throw 'Authenticated subscription tenant differs from the verified configuration.' }
$activeContext = Invoke-AzureJson -Arguments @('account', 'show')
if($activeContext.tenantId -ne $configuration.TenantId) { throw 'The active CLI tenant differs from the verified tenant. Select the intended administrative context before configuring Entra.' }
$scope = $configuration.ProfileResourceId
$expectedAccount = "/subscriptions/$($configuration.SubscriptionId)/resourceGroups/rg-code-signing/providers/Microsoft.CodeSigning/codeSigningAccounts/kof22signing"
if($scope -ne "$expectedAccount/certificateProfiles/$($configuration.CertificateProfileName)")
{
   throw 'Refusing to grant signing permission outside the verified existing certificate profile.'
}
# Refresh the profile to reject stale or edited configuration before any mutation.
$deployed = Invoke-AzureJson -Arguments @('rest', '--method', 'GET', '--url', "https://management.azure.com${scope}?api-version=2025-10-13")
if($deployed.properties.status -ne 'Active' -or $deployed.properties.profileType -ne 'PublicTrust' -or $configuration.PublisherSubject -notin @($deployed.properties.certificates | ForEach-Object subjectName))
{
   throw 'The active deployed publisher profile no longer matches the verified configuration.'
}
$roles = @(Invoke-AzureJson -Arguments @('role', 'definition', 'list', '--name', 'Artifact Signing Certificate Profile Signer', '--subscription', $configuration.SubscriptionId))
if($roles.Count -ne 1 -or $roles[0].roleType -ne 'BuiltInRole') { throw 'The built-in Artifact Signing Certificate Profile Signer role was not uniquely resolved.' }
$appName = 'github-' + $Repository.Replace('/', '-') + '-artifact-signing'
if($ExistingClientId)
{
   $application = Invoke-AzureJson -Arguments @('ad', 'app', 'show', '--id', $ExistingClientId)
}
else
{
   $applications = @(Invoke-AzureJson -Arguments @('ad', 'app', 'list', '--display-name', $appName) | Where-Object displayName -EQ $appName)
   if($applications.Count -gt 1) { throw 'Multiple matching CI applications exist. Select a reviewed ExistingClientId.' }
   $application = if($applications.Count -eq 1) { $applications[0] } else { $null }
   if($null -eq $application)
   {
      if(-not $PSCmdlet.ShouldProcess($appName, 'Create repository-specific Entra CI application')) { return }
      $application = Invoke-AzureJson -Arguments @('ad', 'app', 'create', '--display-name', $appName, '--sign-in-audience', 'AzureADMyOrg')
   }
}
$principals = @(Invoke-AzureJson -Arguments @('ad', 'sp', 'list', '--filter', "appId eq '$($application.appId)'"))
if($principals.Count -gt 1) { throw 'Multiple service principals were returned.' }
$principal = if($principals.Count -eq 1) { $principals[0] } else { $null }
if($null -eq $principal)
{
   if(-not $PSCmdlet.ShouldProcess($application.appId, 'Create CI service principal')) { return }
   $principal = Invoke-AzureJson -Arguments @('ad', 'sp', 'create', '--id', $application.appId)
}
$subject = "repo:${Repository}:environment:${Environment}"
$issuer = 'https://token.actions.githubusercontent.com'
$audience = 'api://AzureADTokenExchange'
$federations = @(Invoke-AzureJson -Arguments @('ad', 'app', 'federated-credential', 'list', '--id', $application.id))
$matchingTrust = @($federations | Where-Object { $_.subject -eq $subject -and $_.issuer -eq $issuer -and @($_.audiences).Count -eq 1 -and $_.audiences[0] -eq $audience })
if($matchingTrust.Count -eq 0)
{
   $credentialName = 'github-' + $Repository.Split('/')[1].ToLowerInvariant() + '-' + $Environment
   if(@($federations | Where-Object name -EQ $credentialName).Count -gt 0) { throw 'An existing credential with this name has different trust. Review it; do not overwrite it.' }
   if($PSCmdlet.ShouldProcess($subject, 'Add repository and protected-environment OIDC trust'))
   {
      $federationFile = Join-Path ([IO.Path]::GetTempPath()) ('k22-federation-' + [guid]::NewGuid().ToString('N') + '.json')
      try
      {
         @{ name = $credentialName; issuer = $issuer; subject = $subject; audiences = @($audience) } |
            ConvertTo-Json | Set-Content -LiteralPath $federationFile -Encoding utf8NoBOM
         $null = Invoke-AzureJson -Arguments @('ad', 'app', 'federated-credential', 'create', '--id', $application.id, '--parameters', $federationFile)
      }
      finally { if(Test-Path -LiteralPath $federationFile) { Remove-Item -LiteralPath $federationFile -Force } }
   }
}
$roleId = $roles[0].id
$assignments = @(Invoke-AzureJson -Arguments @('role', 'assignment', 'list', '--assignee', $principal.id, '--scope', $scope, '--include-inherited', '--subscription', $configuration.SubscriptionId))
if(@($assignments | Where-Object roleDefinitionId -EQ $roleId).Count -eq 0 -and $PSCmdlet.ShouldProcess($scope, 'Grant Certificate Profile Signer to this CI principal'))
{
   $null = Invoke-AzureJson -Arguments @('role', 'assignment', 'create', '--assignee-object-id', $principal.id, '--assignee-principal-type', 'ServicePrincipal', '--role', $roleId, '--scope', $scope, '--subscription', $configuration.SubscriptionId)
}
[ordered]@{ Repository = $Repository; Environment = $Environment; TenantId = $configuration.TenantId; ClientId = $application.appId; PrincipalId = $principal.id; SubscriptionId = $configuration.SubscriptionId; ProfileScope = $scope; FederatedSubject = $subject } | ConvertTo-Json
