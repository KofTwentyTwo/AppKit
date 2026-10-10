#Requires -Version 7.4
# Copyright (c) 2026 James Maes (KofTwentyTwo)
# SPDX-License-Identifier: MIT
<#
.SYNOPSIS
   Reads the existing signing resource and exports verified, non-secret connection details.
.DESCRIPTION
   Does not create or change Azure resources. Requires an authenticated Azure CLI.
#>
[CmdletBinding()]
param(
   [string] $SubscriptionId = '8ef2479e-3346-4cba-93ad-101c6cd237a0',
   [string] $ResourceGroup = 'rg-code-signing',
   [string] $AccountName = 'kof22signing',
   [string] $ProfileName = '',
   [string] $OutputPath = 'artifacts/signing/verified-configuration.json',
   [string] $AzureCli = 'az',
   [string[]] $AzureCliArguments = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$azureExecutable = $AzureCli
$azureCommandPrefix = $AzureCliArguments

<#
.SYNOPSIS
   Invokes the authenticated CLI and rejects failed or malformed responses.
#>
function Invoke-AzureJson
{
   [CmdletBinding()]
   param([Parameter(Mandatory)] [string[]] $Arguments)

   $json = (& $azureExecutable @azureCommandPrefix @Arguments --output json --only-show-errors) -join "`n"
   if($LASTEXITCODE -ne 0) { throw 'Azure inspection failed. Complete az login in this user context first.' }
   return $json | ConvertFrom-Json
}

$subscription = Invoke-AzureJson -Arguments @('account', 'show', '--subscription', $SubscriptionId)
if($subscription.state -ne 'Enabled' -or $subscription.id -ne $SubscriptionId)
{
   throw 'The requested signing subscription is not enabled or did not match.'
}
$tenantId = [guid]::Parse($subscription.tenantId).ToString()
if($tenantId -eq $SubscriptionId) { throw 'The tenant GUID must not be the subscription ID.' }
$resourceId = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.CodeSigning/codeSigningAccounts/$AccountName"
$apiVersion = '2025-10-13'
$account = Invoke-AzureJson -Arguments @('rest', '--method', 'GET', '--url', "https://management.azure.com${resourceId}?api-version=$apiVersion")
$profiles = Invoke-AzureJson -Arguments @('rest', '--method', 'GET', '--url', "https://management.azure.com${resourceId}/certificateProfiles?api-version=$apiVersion")
if($account.properties.provisioningState -ne 'Succeeded') { throw 'The existing signing account is not provisioned successfully.' }
$endpoint = [uri] $account.properties.accountUri
if($endpoint.Scheme -ne 'https' -or $endpoint.Host -notmatch '^[a-z0-9]+\.codesigning\.azure\.net$')
{
   throw 'The account did not return a valid Artifact Signing regional endpoint.'
}
$certificateProfiles = @($profiles.value | Where-Object { (-not $ProfileName -or $_.name -eq $ProfileName) -and $_.properties.profileType -eq 'PublicTrust' -and $_.properties.status -eq 'Active' })
if($certificateProfiles.Count -ne 1) { throw 'Specify the existing profile name when there is not exactly one Active PublicTrust profile. Do not recreate profiles.' }
$subjects = @($certificateProfiles[0].properties.certificates | ForEach-Object subjectName | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
if($subjects.Count -ne 1)
{
   throw 'The profile does not expose one verified publisher subject. Inspect its certificate records before configuring CI.'
}
$configuration = [ordered]@{
   TenantId               = $tenantId
   SubscriptionId         = $subscription.id
   SubscriptionName       = $subscription.name
   AccountResourceId      = $account.id
   ProfileResourceId      = $certificateProfiles[0].id
   Location               = $account.location
   Endpoint               = $endpoint.AbsoluteUri.TrimEnd('/')
   CodeSigningAccountName = $account.name
   CertificateProfileName = $certificateProfiles[0].name
   PublisherSubject       = $subjects[0]
   VerifiedAtUtc          = [DateTime]::UtcNow.ToString('O')
}
New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($OutputPath))) | Out-Null
$configuration | ConvertTo-Json | Set-Content -LiteralPath $OutputPath -Encoding utf8NoBOM
Write-Output "Verified Azure signing configuration saved to $OutputPath. No resources were changed."
