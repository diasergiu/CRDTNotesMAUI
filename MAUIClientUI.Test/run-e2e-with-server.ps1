<#
.SYNOPSIS
	Starts the Server, waits until it is reachable, runs the End-To-End tests, then stops the Server.

.DESCRIPTION
	The E2E tests under MAUIClientUI.Test\EndToEndServiceTest require a live server on
	http://localhost:5266. This script orchestrates that lifecycle so the tests can be run
	with a single command / launch profile.

.PARAMETER Filter
	Optional VSTest filter. Defaults to all End-To-End tests (FullyQualifiedName~EndToEnd).

.EXAMPLE
	powershell -ExecutionPolicy Bypass -File run-e2e-with-server.ps1
#>
[CmdletBinding()]
param(
	[string]$Filter = "FullyQualifiedName~EndToEnd",
	[string]$ServerUrl = "http://localhost:5266",
	[int]$StartupTimeoutSeconds = 60
)

$ErrorActionPreference = "Stop"

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot   = Resolve-Path (Join-Path $scriptDir "..")
$serverProj = Join-Path $repoRoot "Server\Server.csproj"
$testProj   = Join-Path $scriptDir "MAUIClientUI.Test.csproj"

Write-Host "Starting Server ($serverProj) on $ServerUrl ..." -ForegroundColor Cyan
$server = Start-Process -FilePath "dotnet" `
	-ArgumentList @("run", "--project", "`"$serverProj`"", "--launch-profile", "http") `
	-PassThru -NoNewWindow

try {
	# Wait for the server to accept connections.
	$deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
	$ready = $false
	while ((Get-Date) -lt $deadline) {
		if ($server.HasExited) {
			throw "Server process exited unexpectedly (exit code $($server.ExitCode)) before becoming ready."
		}
		try {
			Invoke-WebRequest -Uri $ServerUrl -UseBasicParsing -TimeoutSec 3 | Out-Null
			$ready = $true
			break
		}
		catch [System.Net.WebException], [System.Net.Http.HttpRequestException] {
			# A response (even 404) means the server is up; only connection failures keep us waiting.
			if ($_.Exception.Response) { $ready = $true; break }
			Start-Sleep -Milliseconds 500
		}
		catch {
			Start-Sleep -Milliseconds 500
		}
	}

	if (-not $ready) {
		throw "Server did not become reachable at $ServerUrl within $StartupTimeoutSeconds seconds."
	}

	Write-Host "Server is up. Running E2E tests (filter: $Filter) ..." -ForegroundColor Green
	dotnet test "$testProj" --configuration Debug `
		--logger "console;verbosity=detailed" `
		--filter "$Filter"
	$testExitCode = $LASTEXITCODE
}
finally {
	if ($server -and -not $server.HasExited) {
		Write-Host "Stopping Server (PID $($server.Id)) ..." -ForegroundColor Cyan
		Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue
	}
}

if ($null -ne $testExitCode) {
	exit $testExitCode
}
exit 1
