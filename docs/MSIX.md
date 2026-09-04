# Packaged MSIX build

The repository builds an unsigned MSIX. No certificate or GitHub Actions
secret is required. Windows requires a trusted digital signature to install
an MSIX, so use this artifact for testing or sign it in a separate release
step.

The packaged build is a self-contained x64 MSIX. It includes the Windows App
SDK runtime. It does not need a separate .NET or Windows App SDK installation.

## Optional: create a self-signed certificate

Run PowerShell on a Windows development machine. Use the same publisher name
as `Publisher` in `BluetoothMonitor.App/Package.appxmanifest`.

```powershell
$publisher = 'CN=nazimkov-dev, O=VN Software, C=US'
$cert = New-SelfSignedCertificate `
  -Type Custom `
  -Subject $publisher `
  -KeyUsage DigitalSignature `
  -FriendlyName 'Bluetooth Monitor MSIX signing' `
  -CertStoreLocation 'Cert:\CurrentUser\My' `
  -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3')

$password = Read-Host 'PFX password' -AsSecureString
Export-PfxCertificate -Cert $cert -FilePath .\BluetoothMonitor-signing.pfx -Password $password
Export-Certificate -Cert $cert -FilePath .\BluetoothMonitor-signing.cer
```

Keep the `.pfx` file and its password private. The `.cer` file contains only
the public certificate and can be shared with users who install the package.

The certificate subject must match the manifest publisher exactly. If the
publisher changes, create a new certificate and update the manifest.

## GitHub Actions

The `Build applications` workflow does not use signing secrets. It creates an
unsigned MSIX artifact and a portable ZIP artifact. It runs from **Actions >
Build applications > Run workflow**, and for tags such as `v1.0.0` it adds both
artifacts to a GitHub release.

Do not commit the PFX file, its password, or a Base64 value to the repository.

## Install the package

The unsigned artifact cannot be installed with `Add-AppxPackage`. Sign the
MSIX with a trusted certificate first. For a self-signed package, install the
public certificate once for each user:

```powershell
Import-Certificate -FilePath .\BluetoothMonitor-signing.cer `
  -CertStoreLocation 'Cert:\CurrentUser\TrustedPeople'
Add-AppxPackage -Path .\BluetoothMonitor-win-x64.msix
```

The certificate used to sign the MSIX must be trusted before
`Add-AppxPackage` runs. To remove the app later, use **Settings > Apps >
Installed apps > Bluetooth Monitor**, or run:

```powershell
Get-AppxPackage -Name BluetoothMonitor.App | Remove-AppxPackage
```
