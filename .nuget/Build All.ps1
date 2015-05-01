[CmdletBinding()]
param( 
	[Parameter(Mandatory = $False)]
	[string] $version = $null,
	[Parameter(Mandatory = $False)]
	[string] $preRelease = $null
)


function OutputCommandLineUsageHelp()
{
	Write-Host "Build all NuGet packages."
	Write-Host "============================"
	Write-Host ">E.g.: Build All.ps1"
	Write-Host ">E.g.: Build All.ps1 -PreRelease pre1"
	Write-Host ">E.g.: Build All.ps1 -Version 1.3.0"
	Write-Host ">E.g.: Build All.ps1 -Version 1.3.0 -PreRelease pre1"
}

function Pause ($Message="Press any key to continue...")
{
	Write-Host -NoNewLine $Message
	$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
	Write-Host ""
}

try 
{
	## Initialise
	## ----------
	$originalBackground = $host.UI.RawUI.BackgroundColor
	$originalForeground = $host.UI.RawUI.ForegroundColor
	$originalLocation = Get-Location
	$packages = (Get-Item "$originalLocation\Definition\*.NuSpec" | % { $_.BaseName })
	
	$host.UI.RawUI.BackgroundColor = [System.ConsoleColor]::Black
	$host.UI.RawUI.ForegroundColor = [System.ConsoleColor]::White
	
	Write-Host "Build All NuGet packages" -ForegroundColor White
	Write-Host "==================================" -ForegroundColor White

	Write-Host "Creating Packages folder" -ForegroundColor Yellow
	if (-Not (Test-Path .\Packages)) {
		mkdir Packages
	}

	## NB - Cleanup destination package folder
	## ---------------------------------------
	Write-Host "Clean destination folders..." -ForegroundColor Yellow
	Remove-Item ".\Packages\*.nupkg" -Recurse -Force -ErrorAction SilentlyContinue
	
	## Spawn off individual build processes...
	## ---------------------------------------
	Set-Location "$originalLocation\Definition" ## Adjust current working directory since scripts are using relative paths
	$packages | ForEach { & ".\Build.ps1" -package $_ -version $version -preRelease $preRelease }
	Write-Host "Build All - Done." -ForegroundColor Green
}
catch 
{
	$baseException = $_.Exception.GetBaseException()
	if ($_.Exception -ne $baseException)
	{
	  Write-Host $baseException.Message -ForegroundColor Magenta
	}
	Write-Host $_.Exception.Message -ForegroundColor Magenta
	Pause
} 
finally 
{
	## Restore original values
	$host.UI.RawUI.BackgroundColor = $originalBackground
	$host.UI.RawUI.ForegroundColor = $originalForeground
	Set-Location $originalLocation
}
Pause # For debugging purpose
