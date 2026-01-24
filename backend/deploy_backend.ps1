$ErrorActionPreference = "Stop"

Write-Host "ShopFlow Backend Deployer" -ForegroundColor Cyan

# Check if Key is already set in current session
if (-not $env:GEMINI_API_KEY) {
    Write-Host "GEMINI_API_KEY is not set in this session." -ForegroundColor Yellow
    $apiKey = Read-Host "Please enter your Gemini API Key (input will be hidden)" -AsSecureString
    $env:GEMINI_API_KEY = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($apiKey))
} else {
    Write-Host "GEMINI_API_KEY found in environment." -ForegroundColor Green
}

Write-Host "Deploying CDK Stack..." -ForegroundColor Cyan
cdk deploy --require-approval never

Write-Host "Deployment Complete." -ForegroundColor Green
