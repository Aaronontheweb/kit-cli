#!/usr/bin/env bash
#
# Kit CLI Installer
# 
# Usage:
#   curl -sSL https://raw.githubusercontent.com/Aaronontheweb/kit-cli/dev/install.sh | bash
#   wget -qO- https://raw.githubusercontent.com/Aaronontheweb/kit-cli/dev/install.sh | bash
#
# Or download and run:
#   ./install.sh
#   ./install.sh --dry-run
#   ./install.sh --beta
#   ./install.sh --uninstall
#   INSTALL_DIR=/custom/path ./install.sh
#

set -e

# Configuration
REPO_OWNER="Aaronontheweb"
REPO_NAME="kit-cli"
BINARY_NAME="kit"
INSTALL_DIR="${INSTALL_DIR:-${HOME}/.local/bin}"
CONFIG_DIR="${HOME}/.config/kit-cli"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Helper functions
info() { echo -e "${GREEN}[INFO]${NC} $1"; }
warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
error() { echo -e "${RED}[ERROR]${NC} $1"; exit 1; }

# Detect OS and Architecture
detect_platform() {
    local os arch
    
    # Detect OS
    case "$(uname -s)" in
        Linux*)  os="linux" ;;
        Darwin*) os="osx" ;;
        MINGW*|MSYS*|CYGWIN*) 
            error "Please use install.ps1 for Windows" ;;
        *)       
            error "Unsupported OS: $(uname -s)" ;;
    esac
    
    # Detect Architecture
    case "$(uname -m)" in
        x86_64|amd64) arch="x64" ;;
        aarch64|arm64) arch="arm64" ;;
        armv7l) arch="arm" ;;
        *) error "Unsupported architecture: $(uname -m)" ;;
    esac
    
    echo "${os}-${arch}"
}

# Check for required tools
check_requirements() {
    local missing_tools=()
    
    command -v curl >/dev/null 2>&1 || missing_tools+=("curl")
    command -v tar >/dev/null 2>&1 || missing_tools+=("tar")
    
    if [ ${#missing_tools[@]} -ne 0 ]; then
        error "Missing required tools: ${missing_tools[*]}\nPlease install them and try again."
    fi
}

# Get latest release version from GitHub
get_latest_version() {
    local include_prerelease="${1:-false}"
    local api_url
    
    if [ "$include_prerelease" = true ]; then
        # Get all releases and find the latest (including pre-releases)
        api_url="https://api.github.com/repos/${REPO_OWNER}/${REPO_NAME}/releases"
    else
        # Get only the latest stable release
        api_url="https://api.github.com/repos/${REPO_OWNER}/${REPO_NAME}/releases/latest"
    fi
    
    local response
    response=$(curl -s "$api_url") || error "Failed to fetch release information"
    
    # Extract version from tag_name
    local version
    if [ "$include_prerelease" = true ]; then
        # Get the first release from the array (most recent)
        version=$(echo "$response" | grep '"tag_name"' | head -1 | sed -E 's/.*"tag_name":\s*"([^"]+)".*/\1/')
    else
        version=$(echo "$response" | grep '"tag_name"' | head -1 | sed -E 's/.*"tag_name":\s*"([^"]+)".*/\1/')
    fi
    
    if [ -z "$version" ]; then
        if [ "$include_prerelease" = false ]; then
            warn "No stable release found, trying pre-releases..."
            version=$(get_latest_version true)
        else
            error "Could not determine latest version"
        fi
    fi
    
    echo "$version"
}

# Download and install binary
install_binary() {
    local version="$1"
    local platform="$2"
    local dry_run="$3"
    
    # Remove 'v' prefix if present for the filename
    local version_clean="${version#v}"
    
    # Build download URL
    local download_url="https://github.com/${REPO_OWNER}/${REPO_NAME}/releases/download/${version}/${BINARY_NAME}-${version_clean}-${platform}.tar.gz"
    local temp_dir
    temp_dir=$(mktemp -d)
    local temp_file="${temp_dir}/${BINARY_NAME}.tar.gz"
    
    info "Downloading ${BINARY_NAME} ${version} for ${platform}..."
    
    # Download with progress bar
    if ! curl -L --progress-bar -o "$temp_file" "$download_url"; then
        rm -rf "$temp_dir"
        error "Failed to download from: $download_url"
    fi
    
    info "Extracting binary..."
    if ! tar -xzf "$temp_file" -C "$temp_dir"; then
        rm -rf "$temp_dir"
        error "Failed to extract archive"
    fi
    
    # Find the binary
    local binary_path="${temp_dir}/${BINARY_NAME}"
    
    if [ ! -f "$binary_path" ]; then
        rm -rf "$temp_dir"
        error "Binary not found in archive"
    fi
    
    # Check if this is a dry run
    if [ "$dry_run" = true ]; then
        info "DRY-RUN: Would install binary to ${INSTALL_DIR}/${BINARY_NAME}"
        info "DRY-RUN: Binary found at: $binary_path"
        
        # Test the binary
        if "$binary_path" --version >/dev/null 2>&1; then
            local test_version=$("$binary_path" --version 2>/dev/null | head -1)
            info "DRY-RUN: Binary test successful: $test_version"
        else
            warn "DRY-RUN: Binary test failed - may not be compatible with this system"
        fi
        
        rm -rf "$temp_dir"
        return 0
    fi
    
    # Create install directory if it doesn't exist
    mkdir -p "$INSTALL_DIR"
    
    # Move binary to install location
    info "Installing binary to ${INSTALL_DIR}/${BINARY_NAME}..."
    mv "$binary_path" "${INSTALL_DIR}/${BINARY_NAME}"
    chmod +x "${INSTALL_DIR}/${BINARY_NAME}"
    
    # Clean up
    rm -rf "$temp_dir"
}

# Check if install directory is in PATH
check_path() {
    if [[ ":$PATH:" != *":$INSTALL_DIR:"* ]]; then
        warn "${INSTALL_DIR} is not in your PATH"
        echo ""
        echo "Add the following line to your shell configuration file (.bashrc, .zshrc, etc.):"
        echo "  export PATH=\"\$PATH:${INSTALL_DIR}\""
        echo ""
        echo "Then reload your shell configuration:"
        echo "  source ~/.bashrc  # or ~/.zshrc"
        echo ""
        echo "Or use the full path to run the binary:"
        echo "  ${INSTALL_DIR}/${BINARY_NAME}"
    else
        info "✓ ${INSTALL_DIR} is in PATH"
    fi
}

# Uninstall Kit CLI
uninstall_kit() {
    echo "==================================="
    echo "  Kit CLI Uninstaller"
    echo "==================================="
    echo ""
    
    local removed_something=false
    local binary_path="${INSTALL_DIR}/${BINARY_NAME}"
    
    # Remove binary
    if [ -f "$binary_path" ]; then
        info "Found Kit CLI at: $binary_path"
        
        # Get version before removal
        local version="unknown"
        if "$binary_path" --version >/dev/null 2>&1; then
            version=$("$binary_path" --version 2>/dev/null | head -1)
            info "Version: $version"
        fi
        
        rm "$binary_path" || error "Failed to remove binary"
        info "Binary removed successfully"
        removed_something=true
    else
        warn "Binary not found at $binary_path"
    fi
    
    # Ask about config removal
    if [ -d "$CONFIG_DIR" ]; then
        echo ""
        echo -n "Remove configuration directory $CONFIG_DIR? [y/N]: "
        read -r response
        if [[ "$response" =~ ^[Yy]$ ]]; then
            rm -rf "$CONFIG_DIR" || error "Failed to remove configuration directory"
            info "Configuration directory removed"
            removed_something=true
        else
            info "Configuration directory preserved"
        fi
    fi
    
    echo ""
    if [ "$removed_something" = true ]; then
        info "Uninstall completed successfully"
    else
        warn "Nothing was removed - Kit CLI may not have been installed"
    fi
    
    # Check if install directory is now empty and removable
    if [ -d "$INSTALL_DIR" ] && [ -z "$(ls -A "$INSTALL_DIR" 2>/dev/null)" ]; then
        echo ""
        echo -n "Remove empty installation directory $INSTALL_DIR? [y/N]: "
        read -r response
        if [[ "$response" =~ ^[Yy]$ ]]; then
            rmdir "$INSTALL_DIR" 2>/dev/null && info "Installation directory removed" || warn "Could not remove installation directory"
        fi
    fi
}

# Main installation flow
main() {
    # Parse command line arguments
    local dry_run=false
    local include_beta=false
    local uninstall=false
    
    for arg in "$@"; do
        case $arg in
            --dry-run|--dry|-n)
                dry_run=true
                ;;
            --beta|--pre|--prerelease)
                include_beta=true
                ;;
            --uninstall|--remove)
                uninstall=true
                ;;
            --help|-h)
                echo "Kit CLI Installer"
                echo ""
                echo "Usage: $0 [OPTIONS]"
                echo ""
                echo "Options:"
                echo "  --dry-run, -n       Download and verify but don't install"
                echo "  --beta, --pre       Include beta/pre-release versions"
                echo "  --uninstall         Remove Kit CLI and optionally config"
                echo "  --help, -h          Show this help message"
                echo ""
                echo "Environment variables:"
                echo "  INSTALL_DIR         Set custom installation directory (default: ~/.local/bin)"
                echo ""
                echo "Examples:"
                echo "  # Install latest stable release"
                echo "  curl -sSL https://raw.githubusercontent.com/${REPO_OWNER}/${REPO_NAME}/dev/install.sh | bash"
                echo ""
                echo "  # Install latest beta/pre-release"
                echo "  curl -sSL https://raw.githubusercontent.com/${REPO_OWNER}/${REPO_NAME}/dev/install.sh | bash -s -- --beta"
                echo ""
                echo "  # Dry run to test installation"
                echo "  curl -sSL https://raw.githubusercontent.com/${REPO_OWNER}/${REPO_NAME}/dev/install.sh | bash -s -- --dry-run"
                echo ""
                echo "  # Uninstall"
                echo "  curl -sSL https://raw.githubusercontent.com/${REPO_OWNER}/${REPO_NAME}/dev/install.sh | bash -s -- --uninstall"
                exit 0
                ;;
        esac
    done
    
    # Handle uninstall
    if [ "$uninstall" = true ]; then
        uninstall_kit
        exit 0
    fi
    
    echo "==================================="
    echo "  Kit CLI Installer"
    echo "==================================="
    echo ""
    
    if [ "$dry_run" = true ]; then
        warn "Running in DRY-RUN mode - will download but not install"
        echo ""
    fi
    
    # Check requirements
    check_requirements
    
    # Detect platform
    local platform
    platform=$(detect_platform)
    info "Detected platform: ${platform}"
    
    # Get latest version
    if [ "$include_beta" = true ]; then
        info "Including pre-release versions..."
    fi
    
    local version
    version=$(get_latest_version "$include_beta")
    info "Latest version: ${version}"
    
    if [ "$include_beta" = true ] && [[ "$version" =~ (beta|rc|alpha|preview) ]]; then
        warn "Installing pre-release version: ${version}"
    fi
    
    # Check if already installed
    if [ -f "${INSTALL_DIR}/${BINARY_NAME}" ]; then
        local current_version
        current_version=$("${INSTALL_DIR}/${BINARY_NAME}" --version 2>/dev/null | grep -oE '[0-9]+\.[0-9]+\.[0-9]+' || echo "unknown")
        warn "Existing installation found (version: ${current_version})"
        
        if [ "$dry_run" = false ]; then
            echo -n "Do you want to overwrite it? [y/N] "
            read -r response
            if [[ ! "$response" =~ ^[Yy]$ ]]; then
                echo "Installation cancelled"
                exit 0
            fi
        fi
    fi
    
    # Install binary
    install_binary "$version" "$platform" "$dry_run"
    
    if [ "$dry_run" = true ]; then
        echo ""
        info "DRY-RUN complete! No changes were made to your system."
        info "To actually install, run without --dry-run flag"
    else
        # Verify installation
        if "${INSTALL_DIR}/${BINARY_NAME}" --version >/dev/null 2>&1; then
            info "✓ Successfully installed ${BINARY_NAME} ${version}"
        else
            error "Installation verification failed"
        fi
        
        # Check PATH
        check_path
        
        echo ""
        echo "Installation complete! 🎉"
        echo ""
        echo "To get started:"
        echo "  ${BINARY_NAME} config set --api-key YOUR_API_KEY"
        echo "  ${BINARY_NAME} --help"
        echo ""
        echo "To check for updates:"
        echo "  ${BINARY_NAME} update"
    fi
}

# Run main function
main "$@"