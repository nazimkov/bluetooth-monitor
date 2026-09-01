# Packaged MSIX build

The packaged build is a self-contained x64 MSIX. It includes the Windows App
SDK runtime. It does not need a separate .NET or Windows App SDK installation.

## Create a self-signed certificate

Run PowerShell on a Windows development machine. Use the same publisher name
as `Publisher` in `BluetoothMonitor.App/Package.appxmanifest`.

```powershell
$publisher = 'CN=nazimkov-dev'
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

## Configure GitHub Actions

Convert the PFX to one line of Base64 and add these repository **Actions
secrets** under **Settings > Secrets and variables > Actions**:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes('.\BluetoothMonitor-signing.pfx')) |
  Set-Clipboard
```

Add:

- `MSIX_CERTIFICATE_BASE64`: the clipboard value.
- `MSIX_CERTIFICATE_PASSWORD`: the PFX password.

The `Build applications` workflow runs from **Actions > Build applications >
Run workflow**. It also runs for tags such as `v1.0.0`. The workflow uploads
the MSIX as an artifact and adds it to a tagged GitHub release.

Do not commit the PFX file, its password, or a Base64 value to the repository.

## Install the package

For a self-signed package, install the public certificate once for each user
who installs the app. In PowerShell:

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
