
# NetShift

![Build](https://img.shields.io/github/actions/workflow/status/BakBeat-Labs/NetShift/deploy.yml?event=push)
![Release](https://img.shields.io/github/v/release/BakBeat-Labs/NetShift)
![License](https://img.shields.io/badge/license-PolyForm%20Internal%20Use%201.0.0-blue)
![Last Commit](https://img.shields.io/github/last-commit/BakBeat-Labs/NetShift)

## Download
The latest release of NetShift is available on the [Releases page](https://github.com/BakBeat-Labs/NetShift/releases).  
Download the `NetShiftInstaller.exe` file directly from GitHub Releases and run it as Administrator to install.
	•	Install
	•	Launch
	•	Apply preset / DHCP
	•	Done

## Overview

NetShift is a Windows utility that simplifies switching IP settings on network adapters. It allows users to quickly toggle between static IP configurations and DHCP using predefined presets — ideal for environments where network settings frequently change.  
The app is designed to be **user-friendly**, **secure**, and **efficient**, minimizing user privilege requirements while maintaining robust functionality.

### Key Features
- **Preset Management**: Save, apply, and delete IP configuration presets.
- **DHCP Support**: Reset adapters to DHCP easily.
- **Network Adapter Detection**: Lists physical adapters (excludes Bluetooth adapters).
- **Registry Fallback**: Preconfigure IP settings when adapters are disconnected.
- **Service-Based Architecture**: A LocalSystem Windows service (NetShiftServiceCpp) handles privileged operations, installed via NetShiftServiceInstaller.
- **Installer**: Self-contained installer with .NET 8 Desktop Runtime handling.

---

## Security Considerations

| Risk | Mitigation |
|:-----|:-----------|
| **Service runs as LocalSystem** | Local-only named pipe, input validation, and code signing. |
| **Code signing** | All released binaries and the installer are signed locally with a hardware-token code signing certificate to preserve trust. |
| **User privilege model** | Main app runs standard user level (`asInvoker`), no UAC prompt. |
| **Log files** | Logs are stored under user-local AppData, access-controlled. |
| **Local network communication** | WCF NetNamedPipeBinding used, locked to localhost only. |

---

## Installation

### Prerequisites
- Windows 10 (x64) or later.
- .NET 8 Desktop Runtime.

### Steps to Install
1. Download the latest `NetShiftInstaller.exe` from the [Releases](https://github.com/BakBeat-Labs/NetShift/releases) page.
2. Run `NetShiftInstaller.exe` as Administrator.
3. Follow the installer prompts.

### Silent Install
For automated deployments:

```bash
NetShiftInstaller.exe /SILENT /NORESTART
```

---

## Usage
1. Launch NetShift from Start Menu or Desktop.
2. Select a network adapter.
3. Apply a preset or manually configure IP settings.
4. Click "Apply" or "Reset to DHCP."
5. Save new presets for quick switching.

---

## Building and Contributing

### Automated Build and Draft Release

This project uses **GitHub Actions** to:
- Build NetShiftMain, NetShiftServiceCpp, and NetShiftServiceInstaller and stamp the version from the tag.
- Upload the **unsigned** build output as the `NetShift-Unsigned-Binaries` workflow artifact (kept 30 days).
- Create a **draft** GitHub Release for the tag.

The pipeline **only triggers** when a version tag (e.g., `v1.0.1`) is pushed. CI does **not** sign anything: the code signing certificate lives on a hardware token, so signing and packaging the installer are done manually (see below). The `Build Test Installer` workflow (manual dispatch) produces an unsigned test installer only.

---

### Releasing (manual signing with hardware token)

Run on a Windows machine with the token attached, the certificate's signing tools (Windows SDK `signtool.exe`), and Inno Setup 6 installed.

1. **Tag and push**: `git tag v1.0.1 && git push origin v1.0.1`, then wait for the *Build Unsigned Binaries and Draft Release* workflow to finish.
2. **Get the build output**: check out the tag, then download the `NetShift-Unsigned-Binaries` artifact from the workflow run and extract it over the repo root, so the files land where `setup.iss` expects them (`NetShiftMain\bin\x64\Release\net8.0-windows\`, `NetShiftServiceCpp\x64\Release\`, `NetShiftServiceInstaller\x64\Release\`).
3. **Sign the binaries** (token PIN prompt will appear):
   ```bash
   signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a NetShiftMain\bin\x64\Release\net8.0-windows\net8.0-windows\NetShiftMain.exe NetShiftServiceCpp\x64\Release\NetShiftServiceCpp.exe NetShiftServiceInstaller\x64\Release\NetShiftServiceInstaller.exe
   ```
   Use your CA's timestamp server and add `/sha1 <thumbprint>` if more than one certificate is available.
4. **Build the installer** from the signed binaries:
   ```bash
   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" NetShiftSetup\setup.iss /DMyAppVersion="1.0.1"
   ```
5. **Sign the installer**:
   ```bash
   signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a artifacts\NetShiftInstaller.exe
   signtool verify /pa /v artifacts\NetShiftInstaller.exe
   ```
6. **Publish**: upload `NetShiftInstaller.exe` to the draft release on GitHub (or `gh release upload v1.0.1 artifacts\NetShiftInstaller.exe`), review the notes, and publish the release.

---

### Manual Local Build

If you want to build manually:

1. **Clone the Repository**:

```bash
git clone https://github.com/BakBeat-Labs/NetShift.git
cd NetShift
```

2. **Install Prerequisites**:
- Visual Studio with .NET desktop workload
- .NET desktop development workload (for NetShiftMain and NetShift.Models).
- Desktop development with C++ workload (for NetShiftServiceCpp and NetShiftServiceInstaller).

3. **Build Projects:**
- Open NetShift.sln in Visual Studio.
- Build the solution in Release|x64 configuration:
- - NetShiftMain (C# WPF app).
- - NetShiftServiceCpp (C++ service).
- - NetShiftServiceInstaller (C++ installer helper).

- The build outputs will be in:
- - NetShiftMain\bin\x64\Release\net8.0-windows\net8.0-windows\NetShiftMain.exe
- - NetShiftServiceCpp\x64\Release\NetShiftServiceCpp.exe
- - NetShiftServiceInstaller\x64\Release\NetShiftServiceInstaller.exe

**Build the Installer:**
Navigate to NetShiftSetup and run the Inno Setup compiler (ISCC.exe) on setup.iss:
```bash
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" NetShiftSetup\setup.iss /DMyAppVersion="1.0.0"
```
The installer (NetShiftInstaller.exe) will be generated in the artifacts directory.

4. **Test Installer**:

```bash
copy NetShift\artifacts\NetShiftInstaller.exe C:\Temp\
cd C:\Temp
NetShiftInstaller.exe
```

---

## Contributing
Pull requests are welcome!  
Please fork the repository, make your changes, and submit a pull request.  
Ensure changes are well-tested and match the project's security-first standards.

---

## How It Works

```text
User --> NetShiftMain.exe (runs as Standard User)
            |
            --> (Named Pipe Communication: \\.\pipe\NetShiftService)
                    --> NetShiftServiceCpp.exe (runs as LocalSystem)
                               |
                               --> Windows Networking Stack (Change IP / DHCP Settings)
```

- The user interacts with a normal UI without needing elevation.
- All privileged operations (IP reconfiguration, registry edits) happen securely through the service.
- The service is isolated, only accessible locally, and installed via NetShiftServiceInstaller.exe.

The service exposes a localhost-only named pipe interface and validates all requests before applying changes.
---

## License
NetShift is source-available under the [PolyForm Internal Use License 1.0.0](LICENSE).  
You may use it free of charge, including within a company for its internal business operations. You may not distribute it, sell it, or incorporate it into other software. Versions released before this change remain available under the MIT License.

If you'd like to support this project:

[![Buy Me A Coffee](https://cdn.buymeacoffee.com/buttons/default-orange.png)](https://www.buymeacoffee.com/Mainframe79)
