<#
Script: create-db.ps1
Purpose: Create the RideMatchingDb and tables on a local SQL Server instance (localhost\SQLEXPRESS)

Run from repository root in an elevated PowerShell session:
  pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\create-db.ps1

#>

[CmdletBinding()]
param()

$server = 'localhost\SQLEXPRESS'
$sqlFile = Join-Path $PSScriptRoot '..\sql\CreateDatabaseAndTables.sql'

if (-not (Test-Path $sqlFile)) {
	Write-Error "SQL file not found: $sqlFile"
	exit 1
}

Write-Host "Using SQL file: $sqlFile"
Write-Host "Target SQL Server: $server"

# Try to run with sqlcmd if available
if (Get-Command sqlcmd -ErrorAction SilentlyContinue) {
	Write-Host "Found sqlcmd, executing script..."
	& sqlcmd -S $server -i $sqlFile
	$rc = $LASTEXITCODE
	if ($rc -eq 0) {
		Write-Host "sqlcmd completed successfully."
	} else {
		Write-Warning "sqlcmd exited with code $rc"
	}
}
else {
	# Try Invoke-Sqlcmd from SqlServer module
	if (-not (Get-Module -ListAvailable -Name SqlServer)) {
		Write-Warning "SqlServer PowerShell module not found. Attempting to install module (requires elevation/Internet)."
		try {
			Install-Module -Name SqlServer -Scope CurrentUser -Force -AllowClobber -ErrorAction Stop
		} catch {
			Write-Error "Failed to install SqlServer module. Please install sqlcmd or the SqlServer PowerShell module and re-run the script."
			Write-Host "Install sqlcmd: https://learn.microsoft.com/sql/tools/sqlcmd-utility"
			exit 2
		}
	}

	Import-Module SqlServer -ErrorAction Stop
	Write-Host "Running script via Invoke-Sqlcmd..."
	try {
		Invoke-Sqlcmd -ServerInstance $server -InputFile $sqlFile -ErrorAction Stop
		Write-Host "Invoke-Sqlcmd completed successfully."
	} catch {
		Write-Error "Invoke-Sqlcmd failed: $_"
		exit 3
	}
}

# Verify database existence
Write-Host "Verifying database exists..."
if (Get-Command sqlcmd -ErrorAction SilentlyContinue) {
	& sqlcmd -S $server -Q "SET NOCOUNT ON; SELECT name FROM sys.databases WHERE name='RideMatchingDb';"
} else {
	try {
		$res = Invoke-Sqlcmd -ServerInstance $server -Query "SET NOCOUNT ON; SELECT name FROM sys.databases WHERE name='RideMatchingDb';"
		if ($res) { $res | Format-Table -AutoSize }
	} catch {
		Write-Warning "Verification query failed: $_"
	}
}

Write-Host "Done. If you see 'RideMatchingDb' in the verification results, the database and tables have been created."
