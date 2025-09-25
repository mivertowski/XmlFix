# PowerShell script for building and signing XmlFix analyzer package
param(
    [string]$CertificatePath = "signing\XmlFix.pfx",
    [string]$CertificatePassword = $env:SIGNING_PASSWORD,
    [string]$Configuration = "Release",
    [string]$OutputDir = "artifacts"
)

# Ensure we have required parameters
if (-not $CertificatePassword) {
    Write-Error "Certificate password is required. Set SIGNING_PASSWORD environment variable or pass -CertificatePassword parameter."
    exit 1
}

if (-not (Test-Path $CertificatePath)) {
    Write-Error "Certificate file not found at: $CertificateePath"
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

Write-Host "Signing package: $($packageFile.Name)" -ForegroundColor Green

# Sign the package
dotnet nuget sign $packageFile.FullName `
    --certificate-path $CertificatePath `
    --certificate-password $CertificatePassword `
    --timestamper "http://timestamp.digicert.com"

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