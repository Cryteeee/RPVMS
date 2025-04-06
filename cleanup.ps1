# Kill all processes that might lock our files
Write-Host "Stopping all related processes..."
$processNames = @(
    "iisexpress",
    "w3wp",
    "dotnet",
    "BlazorApp1*",
    "Microsoft.NET.CoreRuntime*",
    "Microsoft.ServiceHub*",
    "ServiceHub.Host*",
    "ServiceHub.TestWindowStoreHost*",
    "vstest.console*",
    "testhost*"
)

foreach ($processName in $processNames) {
    Get-Process -Name $processName -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "Stopping process: $($_.Name) (ID: $($_.Id))"
        Stop-Process -Id $_.Id -Force
    }
}

# Wait for processes to fully terminate
Start-Sleep -Seconds 3

# Kill any remaining dotnet processes that might be holding onto our files
Write-Host "Checking for remaining dotnet processes..."
Get-Process | Where-Object {$_.Path -like "*dotnet*" -and $_.MainWindowTitle -eq ""} | ForEach-Object {
    Write-Host "Stopping process: $($_.Id)"
    Stop-Process -Id $_.Id -Force
}

# Wait another moment
Start-Sleep -Seconds 2

# Clear IIS Express temporary files
Write-Host "Clearing IIS Express temporary files..."
$iisExpressPath = "$env:USERPROFILE\Documents\IISExpress"
if (Test-Path $iisExpressPath) {
    Remove-Item -Path "$iisExpressPath\*" -Recurse -Force -ErrorAction SilentlyContinue
}

# Define paths to clean
$paths = @(
    "bin",
    "obj",
    "Debug",
    "Release"
)

# Function to handle locked files
function Remove-LockedItem {
    param([string]$path)
    try {
        if (Test-Path $path) {
            Write-Host "Removing $path"
            Remove-Item -Path $path -Recurse -Force -ErrorAction Stop
        }
    }
    catch {
        Write-Host "Failed to remove $path, retrying with alternate method..."
        try {
            # Try alternate removal method
            cmd /c "rd /s /q `"$path`""
        }
        catch {
            Write-Warning "Could not remove $path"
        }
    }
}

Write-Host "Removing build artifacts..."
Get-ChildItem -Path . -Include $paths -Recurse -Directory | ForEach-Object {
    Remove-LockedItem $_.FullName
}

# Specifically target the problematic DLL files
$dllPaths = @(
    ".\BlazorApp1\Client\bin\Debug\net7.0\BlazorApp1.Client.dll",
    ".\BlazorApp1\Shared\bin\Debug\net7.0\BlazorApp1.Shared.dll"
)

foreach ($dllPath in $dllPaths) {
    if (Test-Path $dllPath) {
        Write-Host "Removing locked DLL: $dllPath"
        Remove-LockedItem $dllPath
    }
}

# Drop the database
Write-Host "Dropping database..."
sqlcmd -S "(localdb)\mssqllocaldb" -Q "IF EXISTS(SELECT * FROM sys.databases WHERE name = 'VillageDatabase') BEGIN ALTER DATABASE VillageDatabase SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE VillageDatabase; END"

# Clear NuGet cache
Write-Host "Clearing NuGet cache..."
dotnet nuget locals all --clear

# Clear .NET Core SDK workload cache
Write-Host "Clearing .NET SDK workload cache..."
dotnet workload repair

Write-Host "Cleanup completed. You can now rebuild the solution." 