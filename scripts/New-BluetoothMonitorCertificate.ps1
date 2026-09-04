[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\artifacts\certificate'),
    [string]$Subject = 'CN=nazimkov-dev, O=VN Software, C=US',
    [Parameter(Mandatory)]
    [string]$Password
)

$ErrorActionPreference = 'Stop'
$outputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$certificate = New-SelfSignedCertificate `
    -Type Custom `
    -KeyUsage DigitalSignature `
    -Subject $Subject `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -TextExtension @(
        '2.5.29.37={text}1.3.6.1.5.5.7.3.3',
        '2.5.29.19={text}'
    ) `
    -FriendlyName 'Bluetooth Monitor Development Certificate'

$securePassword = ConvertTo-SecureString $Password -AsPlainText -Force
$pfxPath = Join-Path $outputDirectory 'BluetoothMonitor-signing.pfx'
$cerPath = Join-Path $outputDirectory 'BluetoothMonitor-signing.cer'

Export-PfxCertificate -Cert $certificate -FilePath $pfxPath -Password $securePassword | Out-Null
Export-Certificate -Cert $certificate -FilePath $cerPath | Out-Null

Write-Output "Certificate subject: $($certificate.Subject)"
Write-Output "PFX: $pfxPath"
Write-Output "CER: $cerPath"
Write-Output 'Keep the PFX private. Share only the CER with package users.'
