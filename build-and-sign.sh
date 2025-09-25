#!/bin/bash
# Bash script for building and signing XmlFix analyzer package

set -e

# Configuration
CERTIFICATE_PATH="${1:-signing/XmlFix.pfx}"
CERTIFICATE_PASSWORD="${SIGNING_PASSWORD}"
CONFIGURATION="${2:-Release}"
OUTPUT_DIR="${3:-artifacts}"

# Check requirements
if [ -z "$CERTIFICATE_PASSWORD" ]; then
    echo "Error: Certificate password is required. Set SIGNING_PASSWORD environment variable."
    exit 1
fi

if [ ! -f "$CERTIFICATE_PATH" ]; then
    echo "Error: Certificate file not found at: $CERTIFICATE_PATH"
    exit 1
fi

# Create output directory
mkdir -p "$OUTPUT_DIR"

echo "Building XmlFix.Analyzers in $CONFIGURATION configuration..."

# Clean and build
dotnet clean --configuration "$CONFIGURATION"
dotnet build --configuration "$CONFIGURATION" --no-restore

echo "Creating NuGet package..."

# Create package
dotnet pack XmlFix.Analyzers/XmlFix.Analyzers.csproj \
    --configuration "$CONFIGURATION" \
    --no-build \
    --output "$OUTPUT_DIR"

# Find the generated package
PACKAGE_FILE=$(find "$OUTPUT_DIR" -name "XmlFix.Analyzers.*.nupkg" -type f | head -1)

if [ -z "$PACKAGE_FILE" ]; then
    echo "Error: No package file found in $OUTPUT_DIR"
    exit 1
fi

echo "Signing package: $(basename "$PACKAGE_FILE")"

# Sign the package
dotnet nuget sign "$PACKAGE_FILE" \
    --certificate-path "$CERTIFICATE_PATH" \
    --certificate-password "$CERTIFICATE_PASSWORD" \
    --timestamper "http://timestamp.digicert.com"

echo "Package signed successfully: $PACKAGE_FILE"

# Verify the signature
echo "Verifying package signature..."
dotnet nuget verify "$PACKAGE_FILE"

echo "Build and signing complete!"
echo "Package location: $PACKAGE_FILE"