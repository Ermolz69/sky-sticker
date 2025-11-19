# Build script for SkySticker project

# Find .sln file
$slnFile = Get-ChildItem -Path . -Filter "*.sln" -ErrorAction SilentlyContinue | Select-Object -First 1

if (-not $slnFile) {
    # Try parent directory
    $slnFile = Get-ChildItem -Path .. -Filter "*.sln" -ErrorAction SilentlyContinue | Select-Object -First 1
}

if ($slnFile) {
    Write-Host "Found solution file: $($slnFile.FullName)"
    # Use dotnet build instead of MSBuild for better cross-platform support
    dotnet build $slnFile.FullName
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
} else {
    # Fallback to .csproj if no .sln found
    $csprojFile = Get-ChildItem -Path . -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($csprojFile) {
        Write-Host "No .sln file found, building .csproj: $($csprojFile.FullName)"
        dotnet build $csprojFile.FullName
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    } else {
        Write-Error "No .sln or .csproj file found!"
        exit 1
    }
}

