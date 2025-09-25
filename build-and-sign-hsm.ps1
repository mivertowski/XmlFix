# PowerShell script for building and signing with hardware token/HSM
param(
    [string]$CertificateSubject,
    [string]$CertificateThumbprint,
    [string]$CertificateStoreName = "My",
    [string]$Configuration = "Release",
    [string]$OutputDir = "artifacts",
    [switch]$UsePkcs11,
    [string]$Pkcs11Provider,
    [string]$CertificateFile
)

# Ensure we have either subject name or thumbprint
if (-not $CertificateSubject -and -not $CertificateThumbprint -and -not $CertificateFile) {
    Write-Error "Must specify either -CertificateSubject, -CertificateThumbprint, or -CertificateFile"
    Write-Host "Examples:"
    Write-Host "  .\build-and-sign-hsm.ps1 -CertificateSubject 'CN=Your Company Name'"
    Write-Host "  .\build-and-sign-hsm.ps1 -CertificateThumbprint '1234567890ABCDEF...'"
    Write-Host "  .\build-and-sign-hsm.ps1 -CertificateFile 'signing\certificate.cer' -UsePkcs11"
    exit 1
}

# Create output directory
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Write-Host "Building XmlFix.Analyzers in $Configuration configuration..." -ForegroundColor Green

# Clean and build
dotnet clean --configuration $Configuration
dotnet build --configuration $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}

Write-Host "Creating NuGet package..." -ForegroundColor Green

# Create package
dotnet pack XmlFix.Analyzers\XmlFix.Analyzers.csproj --configuration $Configuration --no-build --output $OutputDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "Package creation failed"
    exit 1
}

# Find the generated package
$packageFile = Get-ChildItem -Path $OutputDir -Filter "XmlFix.Analyzers.*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $packageFile) {
    Write-Error "No package file found in $OutputDir"
    exit 1
}

Write-Host "Signing package with hardware token: $($packageFile.Name)" -ForegroundColor Green

# Choose signing method based on parameters
if ($UsePkcs11 -and $CertificateFile) {
    # Method 1: Using PKCS#11 with SignTool
    Write-Host "Using PKCS#11 signing..." -ForegroundColor Yellow

    if (-not $Pkcs11Provider) {
        # Common PKCS#11 providers
        $commonProviders = @(
            "SafeNet Smart Card Key Storage Provider",
            "Microsoft Smart Card Key Storage Provider",
            "YubiKey Smart Card Key Storage Provider",
            "eToken Base Cryptographic Provider"
        )

        foreach ($provider in $commonProviders) {
            try {
                & signtool sign /f $CertificateFile /csp $provider /fd SHA256 /tr "http://timestamp.digicert.com" /td SHA256 $packageFile.FullName
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "Successfully signed with provider: $provider" -ForegroundColor Green
                    break
                }
            }
            catch {
                continue
            }
        }
    }
    else {
        & signtool sign /f $CertificateFile /csp $Pkcs11Provider /fd SHA256 /tr "http://timestamp.digicert.com" /td SHA256 $packageFile.FullName
    }
}
elseif ($CertificateThumbprint) {
    # Method 2: Using certificate thumbprint from store
    Write-Host "Using certificate store with thumbprint..." -ForegroundColor Yellow

    dotnet nuget sign $packageFile.FullName `
        --certificate-store-name $CertificateStoreName `
        --certificate-fingerprint $CertificateThumbprint `
        --timestamper "http://timestamp.digicert.com"
}
elseif ($CertificateSubject) {
    # Method 3: Using certificate subject name from store
    Write-Host "Using certificate store with subject name..." -ForegroundColor Yellow

    dotnet nuget sign $packageFile.FullName `
        --certificate-store-name $CertificateStoreName `
        --certificate-subject-name $CertificateSubject `
        --timestamper "http://timestamp.digicert.com"
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "Package signing failed"
    exit 1
}

Write-Host "Package signed successfully: $($packageFile.FullName)" -ForegroundColor Green

# Verify the signature
Write-Host "Verifying package signature..." -ForegroundColor Yellow
dotnet nuget verify $packageFile.FullName

Write-Host "Build and signing complete!" -ForegroundColor Green
Write-Host "Package location: $($packageFile.FullName)" -ForegroundColor Yellow