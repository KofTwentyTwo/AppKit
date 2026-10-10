<#
.SYNOPSIS
Runs the pinned mutation tool against both UI-independent AppKit libraries.
.DESCRIPTION
Restores the repository-local tool and runs complete project mutations sequentially.
Writes HTML and JSON evidence below artifacts/mutation; surviving mutations must
be reviewed before a minor release. Tool failures cause the script to fail.
.PARAMETER Projects
Libraries to mutate. The default runs both core and updates.
#>
[CmdletBinding()]
param(
   [ValidateSet('KofTwentyTwo.AppKit', 'KofTwentyTwo.AppKit.Updates')]
   [string[]]$Projects = @('KofTwentyTwo.AppKit', 'KofTwentyTwo.AppKit.Updates')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$configuration = Join-Path $PSScriptRoot 'stryker-config.json'
$dotnetExecutable = (Get-Command dotnet -CommandType Application -ErrorAction Stop).Source

Push-Location $repositoryRoot
try
{
   & $dotnetExecutable tool restore
   if($LASTEXITCODE -ne 0)
   {
      throw "Mutation tool restore failed (exit $LASTEXITCODE)."
   }

   Set-Location (Join-Path $repositoryRoot 'tests/KofTwentyTwo.AppKit.Tests')
   foreach($project in $Projects)
   {
      $output = Join-Path $repositoryRoot "artifacts/mutation/$project"
      & $dotnetExecutable stryker --project "$project.csproj" --config-file $configuration --output $output --skip-version-check
      if($LASTEXITCODE -ne 0)
      {
         throw "Mutation testing failed for $project (exit $LASTEXITCODE)."
      }
   }
}
finally
{
   Pop-Location
}
