[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$CertificatePath,

    [Parameter(Mandatory)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$certificateBytes = [IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $CertificatePath))
$certificateBase64 = [Convert]::ToBase64String($certificateBytes)
$certificateName = [IO.Path]::GetFileName($CertificatePath)
# The name is inserted into generated PowerShell source. In a single-quoted
# PowerShell string, a quote is escaped by doubling it.
$escapedCertificateName = $certificateName.Replace("'", "''")

$installer = @"
`$ErrorActionPreference = 'Stop'
`$certificateName = '$escapedCertificateName'
`$certificateBytes = [Convert]::FromBase64String('$certificateBase64')
`$temporaryCertificate = Join-Path ([IO.Path]::GetTempPath()) `$certificateName

try {
    [IO.File]::WriteAllBytes(`$temporaryCertificate, `$certificateBytes)
    Import-Certificate -FilePath `$temporaryCertificate -CertStoreLocation 'Cert:\CurrentUser\TrustedPeople' | Out-Null
    Write-Host 'Bluetooth Monitor certificate installed for the current user.'
}
finally {
    Remove-Item -LiteralPath `$temporaryCertificate -Force -ErrorAction SilentlyContinue
}
"@

$outputFullPath = [IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($outputFullPath)) -Force | Out-Null
[IO.File]::WriteAllText($outputFullPath, $installer, [Text.UTF8Encoding]::new($false))
Write-Output "Installer: $outputFullPath"
