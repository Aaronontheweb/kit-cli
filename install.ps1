# Kit CLI Installer for Windows
#
# Usage:
#   iwr -useb https://raw.githubusercontent.com/Aaronontheweb/kit-cli/dev/install.ps1 | iex
#
# Or download and run:
#   .\install.ps1
#   .\install.ps1 -InstallDir "C:\custom\path"
#   .\install.ps1 -DryRun
#   .\install.ps1 -Beta
#   .\install.ps1 -Uninstall
#

param(
    [string]$InstallDir = "",
    [switch]$Force,
    [switch]$DryRun,
    [switch]$Beta,
    [switch]$Uninstall,
    [switch]$Help
)

$ErrorActionPreference = "Stop"

# Show help if requested
if ($Help) {
    Write-Host "Kit CLI Installer for Windows"
    Write-Host ""
    Write-Host "Usage: .\install.ps1 [OPTIONS]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -InstallDir <path>  Custom installation directory"
    Write-Host "  -Force              Skip confirmation prompts"
    Write-Host "  -DryRun             Download and verify but don't install"
    Write-Host "  -Beta               Include beta/pre-release versions"
    Write-Host "  -Uninstall          Remove Kit CLI and optionally config"
    Write-Host "  -Help               Show this help message"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  # Install latest stable release"
    Write-Host "  .\install.ps1"
    Write-Host ""
    Write-Host "  # Install latest beta"
    Write-Host "  .\install.ps1 -Beta"
    Write-Host ""
    Write-Host "  # Dry run to test installation"
    Write-Host "  .\install.ps1 -DryRun"
    Write-Host ""
    Write-Host "  # Install to custom directory"
    Write-Host "  .\install.ps1 -InstallDir 'C:\tools\kit'"
    Write-Host ""
    Write-Host "  # Uninstall"
    Write-Host "  .\install.ps1 -Uninstall"
    exit 0
}

# Configuration
$RepoOwner = "Aaronontheweb"
$RepoName = "kit-cli"
$BinaryName = "kit.exe"
$ConfigDir = "$env:APPDATA\kit-cli"

# Set default install directory if not provided
if ([string]::IsNullOrEmpty($InstallDir)) {
    $InstallDir = "$env:LOCALAPPDATA\Programs\kit"
}

# Helper functions
function Write-Info { Write-Host "[INFO] $args" -ForegroundColor Green }
function Write-Warn { Write-Host "[WARN] $args" -ForegroundColor Yellow }
function Write-Error { Write-Host "[ERROR] $args" -ForegroundColor Red; exit 1 }

function Get-Platform {
    $arch = if ([Environment]::Is64BitOperatingSystem) { "x64" } else { "x86" }
    return "win-$arch"
}

function Get-LatestVersion {
    param([bool]$IncludeBeta = $false)
    
    $headers = @{
        "User-Agent" = "kit-cli-installer"
    }
    
    try {
        if ($IncludeBeta) {
            Write-Info "Fetching latest release information (including pre-releases)..."
            $apiUrl = "https://api.github.com/repos/$RepoOwner/$RepoName/releases"
            $releases = Invoke-RestMethod -Uri $apiUrl -Headers $headers
            
            if ($releases -and $releases.Count -gt 0) {
                # Get the first release (most recent)
                return $releases[0].tag_name
            }
        } else {
            Write-Info "Fetching latest stable release information..."
            $apiUrl = "https://api.github.com/repos/$RepoOwner/$RepoName/releases/latest"
            
            try {
                $release = Invoke-RestMethod -Uri $apiUrl -Headers $headers
                return $release.tag_name
            } catch {
                # No stable release, try pre-releases
                Write-Warn "No stable release found, trying pre-releases..."
                return Get-LatestVersion -IncludeBeta $true
            }
        }
    }
    catch {
        Write-Error "Failed to fetch release information: $_"
    }
    
    Write-Error "Could not determine latest version"
}

function Install-Binary {
    param(
        [string]$Version,
        [string]$Platform,
        [bool]$IsDryRun = $false
    )
    
    # Remove 'v' prefix if present for the filename
    $versionClean = $Version -replace '^v', ''
    
    # Build download URL
    $downloadUrl = "https://github.com/$RepoOwner/$RepoName/releases/download/$Version/$($BinaryName -replace '\.exe$', '')-$versionClean-$Platform.zip"
    
    $tempDir = New-TemporaryFile | ForEach-Object { Remove-Item $_; New-Item -ItemType Directory -Path $_ }
    $tempFile = Join-Path $tempDir "$BinaryName.zip"
    
    Write-Info "Downloading Kit CLI $Version for $Platform..."
    Write-Info "URL: $downloadUrl"
    
    try {
        # Download with progress
        $ProgressPreference = 'Continue'
        Invoke-WebRequest -Uri $downloadUrl -OutFile $tempFile -UseBasicParsing
    }
    catch {
        Remove-Item -Path $tempDir -Recurse -Force
        Write-Error "Failed to download from: $downloadUrl`n$_"
    }
    
    Write-Info "Extracting binary..."
    try {
        Expand-Archive -Path $tempFile -DestinationPath $tempDir -Force
    }
    catch {
        Remove-Item -Path $tempDir -Recurse -Force
        Write-Error "Failed to extract archive: $_"
    }
    
    # Find the binary
    $binaryPath = Join-Path $tempDir $BinaryName
    if (!(Test-Path $binaryPath)) {
        # Binary might be in a subdirectory
        $binaryPath = Get-ChildItem -Path $tempDir -Filter $BinaryName -Recurse | Select-Object -First 1 -ExpandProperty FullName
        
        if (!$binaryPath -or !(Test-Path $binaryPath)) {
            Remove-Item -Path $tempDir -Recurse -Force
            Write-Error "Binary not found in archive"
        }
    }
    
    if ($IsDryRun) {
        Write-Info "DRY-RUN: Would install binary to $InstallDir\$BinaryName"
        Write-Info "DRY-RUN: Binary found at: $binaryPath"
        
        # Test the binary
        try {
            $testOutput = & $binaryPath --version 2>&1
            Write-Info "DRY-RUN: Binary test successful: $testOutput"
        }
        catch {
            Write-Warn "DRY-RUN: Binary test failed - may not be compatible with this system"
        }
        
        Remove-Item -Path $tempDir -Recurse -Force
        return
    }
    
    # Create install directory if it doesn't exist
    if (!(Test-Path $InstallDir)) {
        New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    }
    
    # Move binary to install location
    Write-Info "Installing binary to $InstallDir\$BinaryName..."
    Move-Item -Path $binaryPath -Destination "$InstallDir\$BinaryName" -Force
    
    # Clean up
    Remove-Item -Path $tempDir -Recurse -Force
}

function Test-InPath {
    $paths = $env:PATH -split ';'
    return $paths -contains $InstallDir
}

function Add-ToPath {
    if (!(Test-InPath)) {
        Write-Warn "$InstallDir is not in your PATH"
        Write-Host ""
        
        if (!$DryRun) {
            Write-Host "Would you like to add it to your PATH? [Y/n]: " -NoNewline
            $response = Read-Host
            
            if ($response -eq '' -or $response -match '^[Yy]') {
                # Add to user PATH
                $userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
                if ($userPath) {
                    $newPath = "$userPath;$InstallDir"
                } else {
                    $newPath = $InstallDir
                }
                
                [Environment]::SetEnvironmentVariable("PATH", $newPath, "User")
                $env:PATH = "$env:PATH;$InstallDir"
                
                Write-Info "Added $InstallDir to PATH"
                Write-Info "You may need to restart your terminal for changes to take effect"
            } else {
                Write-Host ""
                Write-Host "To add manually, run:"
                Write-Host '  [Environment]::SetEnvironmentVariable("PATH", "$env:PATH;' + $InstallDir + '", "User")'
                Write-Host ""
                Write-Host "Or use the full path to run the binary:"
                Write-Host "  $InstallDir\$BinaryName"
            }
        }
    } else {
        Write-Info "✓ $InstallDir is already in PATH"
    }
}

function Uninstall-KitCLI {
    Write-Host "===================================" -ForegroundColor Cyan
    Write-Host "  Kit CLI Uninstaller" -ForegroundColor Cyan
    Write-Host "===================================" -ForegroundColor Cyan
    Write-Host ""
    
    $removedSomething = $false
    $binaryPath = Join-Path $InstallDir $BinaryName
    
    # Remove binary
    if (Test-Path $binaryPath) {
        Write-Info "Found Kit CLI at: $binaryPath"
        
        # Get version before removal
        try {
            $version = & $binaryPath --version 2>&1 | Select-Object -First 1
            Write-Info "Version: $version"
        }
        catch {
            # Ignore version check errors
        }
        
        Remove-Item -Path $binaryPath -Force
        Write-Info "Binary removed successfully"
        $removedSomething = $true
    } else {
        Write-Warn "Binary not found at $binaryPath"
    }
    
    # Ask about config removal
    if (Test-Path $ConfigDir) {
        Write-Host ""
        Write-Host "Remove configuration directory ${ConfigDir}? [y/N]: " -NoNewline
        $response = Read-Host
        
        if ($response -match '^[Yy]') {
            Remove-Item -Path $ConfigDir -Recurse -Force
            Write-Info "Configuration directory removed"
            $removedSomething = $true
        } else {
            Write-Info "Configuration directory preserved"
        }
    }
    
    # Check if install directory is empty
    if ((Test-Path $InstallDir) -and (Get-ChildItem $InstallDir).Count -eq 0) {
        Write-Host ""
        Write-Host "Remove empty installation directory ${InstallDir}? [y/N]: " -NoNewline
        $response = Read-Host
        
        if ($response -match '^[Yy]') {
            Remove-Item -Path $InstallDir -Force
            Write-Info "Installation directory removed"
        }
    }
    
    # Remove from PATH if present
    if (Test-InPath) {
        Write-Host ""
        Write-Host "Remove $InstallDir from PATH? [y/N]: " -NoNewline
        $response = Read-Host
        
        if ($response -match '^[Yy]') {
            $userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
            $paths = $userPath -split ';' | Where-Object { $_ -ne $InstallDir }
            $newPath = $paths -join ';'
            [Environment]::SetEnvironmentVariable("PATH", $newPath, "User")
            Write-Info "Removed from PATH"
        }
    }
    
    Write-Host ""
    if ($removedSomething) {
        Write-Info "Uninstall completed successfully"
    } else {
        Write-Warn "Nothing was removed - Kit CLI may not have been installed"
    }
}

# Main execution
if ($Uninstall) {
    Uninstall-KitCLI
    exit 0
}

Write-Host "===================================" -ForegroundColor Cyan
Write-Host "  Kit CLI Installer" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan
Write-Host ""

if ($DryRun) {
    Write-Warn "Running in DRY-RUN mode - will download but not install"
    Write-Host ""
}

# Detect platform
$platform = Get-Platform
Write-Info "Detected platform: $platform"

# Get latest version
$version = Get-LatestVersion -IncludeBeta $Beta
Write-Info "Latest version: $version"

if ($Beta -and $version -match "(beta|rc|alpha|preview)") {
    Write-Warn "Installing pre-release version: $version"
}

# Check if already installed
$existingBinary = Join-Path $InstallDir $BinaryName
if (Test-Path $existingBinary) {
    try {
        $currentVersion = & $existingBinary --version 2>&1 | Select-Object -First 1
        Write-Warn "Existing installation found: $currentVersion"
    }
    catch {
        Write-Warn "Existing installation found (version unknown)"
    }
    
    if (!$DryRun -and !$Force) {
        Write-Host "Do you want to overwrite it? [y/N]: " -NoNewline
        $response = Read-Host
        
        if ($response -notmatch '^[Yy]') {
            Write-Host "Installation cancelled"
            exit 0
        }
    }
}

# Install binary
Install-Binary -Version $version -Platform $platform -IsDryRun $DryRun

if ($DryRun) {
    Write-Host ""
    Write-Info "DRY-RUN complete! No changes were made to your system."
    Write-Info "To actually install, run without -DryRun flag"
} else {
    # Verify installation
    $installedBinary = Join-Path $InstallDir $BinaryName
    if (Test-Path $installedBinary) {
        try {
            $installedVersion = & $installedBinary --version 2>&1 | Select-Object -First 1
            Write-Info "✓ Successfully installed Kit CLI $version"
        }
        catch {
            Write-Error "Installation verification failed"
        }
    } else {
        Write-Error "Installation failed - binary not found"
    }
    
    # Check PATH
    Add-ToPath
    
    Write-Host ""
    Write-Host "Installation complete! 🎉" -ForegroundColor Green
    Write-Host ""
    Write-Host "To get started:"
    Write-Host "  kit config set --api-key YOUR_API_KEY"
    Write-Host "  kit --help"
    Write-Host ""
    Write-Host "To check for updates:"
    Write-Host "  kit update"
}