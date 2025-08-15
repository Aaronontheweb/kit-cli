# Kit CLI Installation Script for Windows

param(
    [string]$InstallDir = "$env:LOCALAPPDATA\Programs\KitCLI"
)

$ErrorActionPreference = "Stop"

$repo = "Aaronontheweb/kit-cli"
$binaryName = "kit.exe"

Write-Host "Installing Kit CLI..." -ForegroundColor Cyan

# Detect architecture
$arch = if ([Environment]::Is64BitOperatingSystem) { "x64" } else { "x86" }
$platform = "win-$arch"

# Create installation directory
if (!(Test-Path $InstallDir)) {
    Write-Host "Creating installation directory: $InstallDir"
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
}

# Get latest release
Write-Host "Fetching latest release..."
try {
    $releases = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/latest"
    $asset = $releases.assets | Where-Object { $_.name -like "*$platform*" } | Select-Object -First 1
    
    if ($null -eq $asset) {
        Write-Host "No pre-built binary found for $platform" -ForegroundColor Yellow
        Write-Host "Attempting to build from source..."
        
        # Check if .NET SDK is installed
        $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
        if ($null -eq $dotnet) {
            Write-Host ".NET SDK is not installed. Please install .NET 9 SDK first." -ForegroundColor Red
            Write-Host "Visit: https://dotnet.microsoft.com/download/dotnet/9.0"
            exit 1
        }
        
        # Clone and build
        $tempDir = New-TemporaryFile | ForEach-Object { Remove-Item $_; New-Item -ItemType Directory -Path $_ }
        Push-Location $tempDir
        
        try {
            Write-Host "Cloning repository..."
            git clone --depth 1 "https://github.com/$repo.git" kit-cli
            Set-Location kit-cli\src\KitCLI
            
            Write-Host "Building Kit CLI..."
            dotnet publish -c Release /p:PublishAot=true /p:PublishSingleFile=true --self-contained -r $platform
            
            $binaryPath = Get-ChildItem -Path . -Filter "kit.exe" -Recurse | Where-Object { $_.DirectoryName -like "*publish*" } | Select-Object -First 1
            
            if ($null -eq $binaryPath) {
                throw "Failed to build Kit CLI"
            }
            
            Copy-Item $binaryPath.FullName "$InstallDir\$binaryName" -Force
        }
        finally {
            Pop-Location
            Remove-Item $tempDir -Recurse -Force
        }
    }
    else {
        # Download pre-built binary
        $downloadUrl = $asset.browser_download_url
        Write-Host "Downloading Kit CLI from $downloadUrl..."
        
        $tempFile = [System.IO.Path]::GetTempFileName()
        Invoke-WebRequest -Uri $downloadUrl -OutFile "$tempFile.zip"
        
        # Extract
        Write-Host "Extracting..."
        Expand-Archive -Path "$tempFile.zip" -DestinationPath $InstallDir -Force
        
        # Clean up
        Remove-Item "$tempFile.zip"
    }
}
catch {
    Write-Host "Error during installation: $_" -ForegroundColor Red
    exit 1
}

# Add to PATH if not already there
$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($userPath -notlike "*$InstallDir*") {
    Write-Host "Adding Kit CLI to PATH..."
    $newPath = "$userPath;$InstallDir"
    [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
    $env:Path = "$env:Path;$InstallDir"
    
    Write-Host "Kit CLI has been added to your PATH." -ForegroundColor Green
    Write-Host "You may need to restart your terminal for the changes to take effect." -ForegroundColor Yellow
}

# Verify installation
$kitPath = Join-Path $InstallDir $binaryName
if (Test-Path $kitPath) {
    Write-Host "`nKit CLI installed successfully!" -ForegroundColor Green
    Write-Host ""
    
    # Try to run version command
    try {
        & $kitPath --version
    }
    catch {
        Write-Host "Version: (run 'kit --version' to check)" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "Get started with: kit --help" -ForegroundColor Cyan
}
else {
    Write-Host "Installation may have failed. Please check $InstallDir" -ForegroundColor Red
    exit 1
}