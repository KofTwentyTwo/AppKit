# Windows release signing

AppKit uses the existing KofTwentyTwo Azure Artifact Signing service. Do not create
another tenant, signing account, publisher validation or certificate profile. Never
export a PFX or put the administrative account's password in CI.

## Current implementation and evidence

The owner reports completed publisher validation and an active Public Trust profile.
The subscription observed during setup is `8ef2479e-3346-4cba-93ad-101c6cd237a0`,
account `kof22signing`, resource group `rg-code-signing`. The tenant GUID, actual
region/endpoint, final profile name and publisher subject still require a successful
authenticated resource lookup. `releases` and East US were setup expectations.

Local tooling and the shared builder changes are prepared. The latter are isolated
on standards branch `fix/artifact-signing-appkit`, in this checkout's ignored
`artifacts/standards-signing/` worktree. The published standards v0.1.2 NuGet builder
does not support signing, so AppKit's next release deliberately fails preflight until
the reviewed shared change is published and the caller is updated. Do not sign or
repack packages after their checksums, attestations or immutable release are created.

Verified locally: 26 signing/package checks and six administrative-boundary tests;
Microsoft's `Microsoft.ArtifactSigning.Client` 1.0.146 author/repository signatures;
real `signtool verify /pa /all /v` on the installed Microsoft-signed SignTool; rejection
of an unsigned AppKit DLL. **No AppKit Azure signature or signed release is proven.**
The attempted CLI device login ended with authorization pending. No Azure identities,
credentials, roles or GitHub signing variables were changed.

## Reference from gclo

The inspected gclo release environment uses tenant
`4ebeb0ad-5782-487a-a1b4-5080255774cf`, the subscription above, account
`kof22signing`, profile `releases` and `https://eus.codesigning.azure.net`.
These are observed configuration values, not a deployed-resource lookup. Its
publisher check uses `James Maes`; obtain the full certificate subject from the
profile before configuring AppKit. Do not copy gclo's client ID without checking
AppKit's federated trust and profile-scoped signer permission.

[Standards v1.0.0](https://github.com/KofTwentyTwo/standards/releases/tag/v1.0.0)
publishes gclo's Velopack signing fix: preserve valid existing signatures, allow
more time for signing, and invoke SignTool/dlib through `--signTemplate`.
Its NuGet builder still lacks the signing inputs AppKit needs. The isolated
AppKit builder additionally selects explicit owned files, refuses invalid
existing signatures, verifies each newly signed helper, and checks final
publisher, timestamp, archive bytes and embedded installer payload before
publication. Missing third-party dependencies now fail archive verification,
with only Velopack's default runtime-helper exclusions allowed.

Five regression tests exercise the actual signing wrapper without credentials.
The inspected [gclo beta.3 run](https://github.com/KofTwentyTwo/gclo/actions/runs/38087007507)
passed OIDC, signing, packaging and its pre-publication signature check. The run
ultimately failed because its separate post-publication verification job could
not read the publisher variable. Require all signing variables in the protected
release environment before building; do not defer this check until publication.
Successful gclo signing is reference evidence; it does not prove AppKit's
identity, binaries or final release. Keep signing access confined to trusted
release jobs and retain the required real AppKit verification below.

## Verify the existing resource

Sign in interactively with `koftwentytwo@outlook.com`, using the existing directory
domain only to select the tenant:

```powershell
az login --tenant koftwentytwooutlook.onmicrosoft.com
./build/Get-ArtifactSigningConfiguration.ps1
```

If using the isolated CLI installed during this work:

```powershell
$cliPrefix = @('tool', 'run', '--python', '3.13', '--prerelease', 'allow', '--with', 'setuptools<81', '--from', 'azure-cli==2.91.0', 'az')
uv @cliPrefix login --tenant koftwentytwooutlook.onmicrosoft.com
./build/Get-ArtifactSigningConfiguration.ps1 -AzureCli uv -AzureCliArguments $cliPrefix
```

The read-only script retrieves the subscription's tenant GUID and deployed account
URI/location, selects the existing Active PublicTrust profile and reads
`properties.certificates[].subjectName`. It saves non-secret evidence in
`artifacts/signing/verified-configuration.json`. With multiple active profiles, pass
`-ProfileName <existing-name>`. Missing or ambiguous certificate subjects stop setup;
inspect the existing profile's certificate records rather than inventing a subject.

## Configure repository-specific authentication

An administrator needs permission to manage the selected Entra application and
assign the built-in signer role at the existing certificate profile. Owner or
Contributor alone is insufficient to sign.

```powershell
$configPath = 'artifacts/signing/verified-configuration.json'
./build/New-ArtifactSigningIdentity.ps1 -ConfigurationPath $configPath -WhatIf
$identity = ./build/New-ArtifactSigningIdentity.ps1 -ConfigurationPath $configPath | ConvertFrom-Json
```

Add `-AzureCli uv -AzureCliArguments $cliPrefix` when using the isolated CLI. To reuse
a reviewed existing CI application, pass `-ExistingClientId <application-client-id>`.
The script creates a repository-specific application/service principal only if
needed, adds trust for `repo:KofTwentyTwo/AppKit:environment:release`, and assigns
**Artifact Signing Certificate Profile Signer** at the certificate profile scope.
It preserves other applications, credentials and assignments, and rejects conflicting
trust. Coordinate application reuse with other app maintainers before running it.

Each generated application needs its own repository subject and reviewed identity.
Repeat with `-Repository Owner/NewApp`; do not give arbitrary template consumers
access to KofTwentyTwo's signing service.

Set these non-secret variables in GitHub's protected **release** environment, using
the verified configuration and returned identity:

```powershell
$config = Get-Content $configPath -Raw | ConvertFrom-Json
$variables = @{
   AZURE_TENANT_ID = $config.TenantId
   AZURE_CLIENT_ID = $identity.ClientId
   AZURE_SUBSCRIPTION_ID = $config.SubscriptionId
   ARTIFACT_SIGNING_ENDPOINT = $config.Endpoint
   ARTIFACT_SIGNING_ACCOUNT = $config.CodeSigningAccountName
   ARTIFACT_SIGNING_PROFILE = $config.CertificateProfileName
   ARTIFACT_SIGNING_SUBJECT = $config.PublisherSubject
}
foreach($entry in $variables.GetEnumerator())
{
   gh variable set $entry.Key --repo KofTwentyTwo/AppKit --env release --body $entry.Value
}
```

AppKit's environment currently permits only `v*` tags. Retain that protection and
the tag-on-main gate. Signing jobs use `contents: read`, `id-token: write` and the
official `azure/login` v3 and `azure/artifact-signing-action` v2 pinned to immutable
commits. Disable every other credential type; use the Azure CLI credential established
by login. Pull requests never receive signing access.

## Enable the shared release builders

Review and publish the prepared standards change through its normal release process.
Update AppKit's shared builder pin to that **published release's full commit SHA**
and version comment. Add the following to the existing release job's `with:` block:

```yaml
sign: true
signing-files: |
  src/KofTwentyTwo.AppKit/bin/Release/net10.0/KofTwentyTwo.AppKit.dll
  src/KofTwentyTwo.AppKit.Updates/bin/Release/net10.0/KofTwentyTwo.AppKit.Updates.dll
  src/KofTwentyTwo.AppKit.WinUI/bin/Release/net10.0-windows10.0.19041.0/KofTwentyTwo.AppKit.WinUI.dll
  src/KofTwentyTwo.AppKit.Wpf/bin/Release/net10.0-windows/KofTwentyTwo.AppKit.Wpf.dll
```

The NuGet builder signs these DLLs before `pack --no-build`, then extracts each final
package and verifies the shipped DLLs and their exact bytes. NuGet ZIP containers and
symbol packages are not Authenticode targets; retain NuGet integrity/provenance checks.

The Velopack builder accepts required owned names or publish-relative patterns.
For the starter's CLI variant, the catalog includes `app/Kof22App.exe`,
`app/Kof22App.dll`, `app/Kof22App.Core.dll`, the three consumed AppKit DLLs and
`cli/kof22-app.exe`; adapt these to the generated names. The no-CLI variant omits
the last entry. Preserve all other binary bytes and signatures. The catalog gate
rejects malformed owned or preserved `.exe`/`.dll` inputs before catalog creation;
unchanged hashes alone cannot prove that the source was executable. MSI/MSIX follow
their format-specific package/signature checks. The packaging hook
signs branded Squirrel/execution stubs and the final installer after bundling; final
verification covers portable/CLI ZIPs, full update packages, installer signatures and
the installer's embedded full-package hash. Manifests, checksums and attestations
follow signing. Delta upgrade reconstruction still needs the installed-channel test
tracked in AppKit #11. Future MSIX manifests must use the verified publisher subject.

## Local signing and the required real test

Install Microsoft's [Artifact Signing Client Tools](https://learn.microsoft.com/azure/artifact-signing/how-to-signing-integrations)
(`winget install -e --id Microsoft.Azure.ArtifactSigningClientTools`), the x64 Windows
SDK SignTool, .NET 8 runtime and required Visual C++ runtime. An already verified client
copy is at `artifacts/signing/client-tools/1.0.146/bin/x64/Azure.CodeSigning.Dlib.dll`.
The discovered SignTool is `C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe`.
Use `Get-WindowsSignTool` to resolve other machines' SDK versions.

The local identity must have the profile signer role. If the administrative user lacks
it, an authorized administrator must assign that exact built-in role at the verified
profile; subscription Owner access is not a substitute. Ensure an authenticated `az`
is available on PATH for the client's `AzureCliCredential`.

```powershell
Import-Module ./build/WindowsSigning.psm1 -Force
# Build Release first; enumerate owned DLLs using the four exact paths above.
$owned = @('src/KofTwentyTwo.AppKit/bin/Release/net10.0/KofTwentyTwo.AppKit.dll')
New-WindowsSigningCatalog -Root . -Patterns $owned -CatalogPath artifacts/signing/catalog.txt -ManifestPath artifacts/signing/manifest.json -ExpectedSubject $config.PublisherSubject
./build/Invoke-WindowsSigning.ps1 -ConfigurationPath $configPath -ManifestPath artifacts/signing/manifest.json -DlibPath artifacts/signing/client-tools/1.0.146/bin/x64/Azure.CodeSigning.Dlib.dll
```

The client metadata contains `Endpoint`, `CodeSigningAccountName` and
`CertificateProfileName`. Signing always uses `/fd SHA256`,
`/tr http://timestamp.acs.microsoft.com` and `/td SHA256`. Existing valid signatures
are preserved; invalid existing signatures fail.

For the representative test, sign all four Release DLLs, pack without rebuilding,
then run `Assert-WindowsDistribution` against the final NuGet directory. For apps,
run the signed Velopack path and verify the final installer and extracted binaries:

```powershell
signtool verify /pa /all /v <artifact.exe-or-dll>
Assert-WindowsDistribution -Directory artifacts/nuget -ManifestPath artifacts/signing/manifest.json -ExpectedSubject $config.PublisherSubject
```

Record the deployed profile subject, SignTool trust/timestamp output, final asset
hashes and CI run before closing AppKit #12 or EX-0002. An Active profile alone is
not proof that signing works. Pushes, merges and release publication require their
existing owner authorization.
