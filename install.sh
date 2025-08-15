#!/usr/bin/env bash

set -e

REPO="Aaronontheweb/kit-cli"
INSTALL_DIR="/usr/local/bin"
BINARY_NAME="kit"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "Installing Kit CLI..."

# Detect OS and architecture
OS=$(uname -s | tr '[:upper:]' '[:lower:]')
ARCH=$(uname -m)

case "$OS" in
    linux*)
        PLATFORM="linux"
        ;;
    darwin*)
        PLATFORM="osx"
        ;;
    *)
        echo -e "${RED}Unsupported operating system: $OS${NC}"
        exit 1
        ;;
esac

case "$ARCH" in
    x86_64|amd64)
        ARCH="x64"
        ;;
    arm64|aarch64)
        ARCH="arm64"
        ;;
    *)
        echo -e "${RED}Unsupported architecture: $ARCH${NC}"
        exit 1
        ;;
esac

RELEASE_TAG="$PLATFORM-$ARCH"

# Get latest release URL
echo "Fetching latest release..."
LATEST_RELEASE=$(curl -s "https://api.github.com/repos/$REPO/releases/latest" | grep browser_download_url | grep "$RELEASE_TAG" | cut -d '"' -f 4)

if [ -z "$LATEST_RELEASE" ]; then
    echo -e "${YELLOW}No pre-built binary found for $PLATFORM-$ARCH${NC}"
    echo "Building from source..."
    
    # Check if .NET SDK is installed
    if ! command -v dotnet &> /dev/null; then
        echo -e "${RED}.NET SDK is not installed. Please install .NET 9 SDK first.${NC}"
        echo "Visit: https://dotnet.microsoft.com/download/dotnet/9.0"
        exit 1
    fi
    
    # Clone and build
    TEMP_DIR=$(mktemp -d)
    cd "$TEMP_DIR"
    
    echo "Cloning repository..."
    git clone --depth 1 "https://github.com/$REPO.git" kit-cli
    cd kit-cli/src/KitCLI
    
    echo "Building Kit CLI..."
    dotnet publish -c Release /p:PublishAot=true /p:PublishSingleFile=true --self-contained -r "$PLATFORM-$ARCH"
    
    # Find the built binary
    BINARY_PATH=$(find . -name kit -type f -path "*/publish/*" | head -1)
    
    if [ -z "$BINARY_PATH" ]; then
        echo -e "${RED}Failed to build Kit CLI${NC}"
        exit 1
    fi
else
    # Download pre-built binary
    TEMP_DIR=$(mktemp -d)
    cd "$TEMP_DIR"
    
    echo "Downloading Kit CLI from $LATEST_RELEASE..."
    curl -sL "$LATEST_RELEASE" -o kit.tar.gz
    tar -xzf kit.tar.gz
    BINARY_PATH="./kit"
fi

# Check if we need sudo for installation
if [ -w "$INSTALL_DIR" ]; then
    SUDO=""
else
    SUDO="sudo"
    echo "Installation requires administrator privileges..."
fi

# Install the binary
echo "Installing to $INSTALL_DIR/$BINARY_NAME..."
$SUDO mv "$BINARY_PATH" "$INSTALL_DIR/$BINARY_NAME"
$SUDO chmod +x "$INSTALL_DIR/$BINARY_NAME"

# Clean up
cd /
rm -rf "$TEMP_DIR"

# Verify installation
if command -v kit &> /dev/null; then
    echo -e "${GREEN}Kit CLI installed successfully!${NC}"
    echo ""
    kit --version
    echo ""
    echo "Get started with: kit --help"
else
    echo -e "${YELLOW}Kit CLI was installed to $INSTALL_DIR but is not in your PATH${NC}"
    echo "Add $INSTALL_DIR to your PATH or use the full path: $INSTALL_DIR/$BINARY_NAME"
fi