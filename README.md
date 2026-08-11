# Archura Windrop

Archura Windrop is a Windows tray application that receives content from iPhone, iPad, and macOS over the local network by presenting the PC as an AirPrint destination.

Instead of printing the incoming job, Windrop saves it, converts it when appropriate, copies a useful representation to the Windows clipboard, and keeps a local history. It is designed to feel like a lightweight receive bridge without attempting to implement Apple's private AirDrop/AWDL protocol.

> [!IMPORTANT]
> Windrop uses the Apple **Share → Print** workflow. It is not a native AirDrop protocol implementation.

## Features

- AirPrint-compatible `_ipp._tcp.local` and `_universal._sub._ipp._tcp.local` discovery
- Raw TCP IPP 1.1/2.0 server with:
  - `Get-Printer-Attributes`
  - `Validate-Job`
  - `Print-Job`
  - `Get-Jobs`
  - `Get-Job-Attributes`
- Correct `Expect: 100-continue`, fixed-length, and chunked HTTP body handling
- 200 MB configurable incoming-document limit
- Automatic PDF content classification:
  - text-only PDF → UTF-8 `.txt`
  - visual or mixed PDF → PNG or JPEG
- Per-document output prompt: **Automatic**, **Save as image**, or **Text only**
- Lossless PNG output at up to 600 DPI
- JPEG output at maximum encoder quality
- Multi-page PDF conversion
- Clipboard output as Unicode text, bitmap, and/or file drop (`CF_HDROP`)
- Configurable `Documents\Windrop\Received` storage folder
- Receive history, tray notifications, optional approval prompt, and Windows startup registration
- Local JSON settings; no cloud service and no outbound document upload

## Supported languages

The application UI, tray menu, notifications, approval dialog, and PDF output prompt support:

- English
- Türkçe
- Deutsch
- Español
- Русский
- 简体中文

Select a language under **Settings → Language**. The choice is persisted locally and applied to the running application.

## Requirements

- Windows 10 version 2004 / build 19041 or newer
- .NET 8 Desktop Runtime for framework-dependent builds
- iPhone, iPad, or Mac on the same local network as the PC
- Inbound TCP `8631` and UDP `5353` access from the local subnet

iOS Simulator cannot be used for AirPrint/mDNS validation. A physical Apple device is required.

## Build from source

```powershell
dotnet restore Windrop.slnx
dotnet build Windrop.slnx
dotnet run --project src\Windrop.App\Windrop.App.csproj
```

Install the local-subnet firewall rules from an elevated PowerShell window:

```powershell
.\scripts\Install-FirewallRules.ps1 -TcpPort 8631
```

Remove them later with:

```powershell
.\scripts\Remove-FirewallRules.ps1
```

The firewall rules apply only to `LocalSubnet` on Private and Public Windows network profiles.

## Receive from an Apple device

1. Start Archura Windrop on Windows.
2. Confirm that the Apple device and PC are on the same LAN/Wi-Fi network.
3. On iPhone or iPad, open an item and select **Share → Print**.
4. Select `Archura Bridge (...)` from the printer list.
5. Send the print job.
6. Choose the desired PDF output when Windrop asks, or configure a permanent mode under **PDF handling**.

The original source PDF is retained as a fallback. The selected text or image output becomes the primary history and clipboard item.

## PDF output modes

| Mode | Behavior |
|---|---|
| Ask every time | Shows a 10-second output prompt, then falls back to Automatic |
| Automatic | Text-only PDFs become `.txt`; visual/mixed PDFs become PNG or JPEG |
| Always image | Renders every PDF page using the configured image format and DPI |
| Text only | Creates `.txt` when an extractable PDF text layer is available |

PNG at 600 DPI is the default and highest-quality setting. Higher quality increases conversion time, memory use, and output size.

## Test

```powershell
dotnet run --project tests\Windrop.ProtocolTests\Windrop.ProtocolTests.csproj
```

The test executable covers IPP encoding/parsing, mDNS response filtering, format detection, settings migration, PDF classification, PNG/JPEG conversion, selected output behavior, `100 Continue`, and chunked `Print-Job` handling.

## Architecture

```text
src/
├── Windrop.Domain          Models, settings, and interfaces
├── Windrop.Data            Atomic JSON settings and history storage
├── Windrop.Infrastructure  mDNS, HTTP/IPP, persistence, PDF analysis/conversion
└── Windrop.App             WPF UI, tray host, clipboard, notifications, localization

tests/
└── Windrop.ProtocolTests   Protocol and integration test executable
```

The tray icon runs on its own STA/WinForms message loop so network traffic and PDF conversion cannot block its context menu. Clipboard operations remain on the interactive WPF STA thread.

## Privacy and network scope

- Documents remain on the Windows PC.
- Windrop does not upload received content to an external service.
- The IPP endpoint is unauthenticated to match AirPrint behavior, so use Windrop only on trusted local networks.
- Firewall rules are restricted to the local subnet.
- Incoming document size is limited to reduce accidental or abusive resource use.

## Troubleshooting

If the bridge does not appear in the printer list:

- verify that Windrop is running and TCP `8631` is listening;
- install the included firewall rules;
- avoid guest Wi-Fi networks with client isolation;
- temporarily disable VPN adapters;
- confirm that the access point allows multicast/Bonjour traffic on UDP `5353`.

Logs and settings are stored under `%LOCALAPPDATA%\Archura\Windrop`.

## Türkçe hızlı başlangıç

Windrop'u başlatın, yönetici PowerShell'inde firewall betiğini çalıştırın ve iPhone ile bilgisayarın aynı ağda olduğundan emin olun. iPhone'da içeriği açıp **Paylaş → Yazdır** yolunu izleyin ve `Archura Bridge (...)` aygıtını seçin. Gelen dosyalar varsayılan olarak `Belgeler\Windrop\Received` klasörüne kaydedilir. Dil, çıktı biçimi, DPI ve kayıt klasörü uygulamanın **Ayarlar** sekmesinden değiştirilebilir.

## Current limitation

URF jobs are preserved safely as files, but direct URF raster-to-bitmap decoding is not implemented yet. PDF, PNG, and JPEG are the preferred advertised paths.
