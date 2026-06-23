# Bluetooth Sentinel 🛡️

**Bluetooth Sentinel** is a lightweight Windows 11 application designed to give you granular control over your Bluetooth connections. It solves the common frustration of devices (like multi-pairing earphones) automatically connecting to your laptop when you intend to use them with your phone or another device.

Developed by me, **cerealicious**, this tool acts as a "watchdog" for your Bluetooth environment, ensuring your devices only connect when you want them to.

## ✨ Features

*   **Auto-Connect Management:** Toggle auto-connect preferences for each paired device individually.
*   **Smart Disconnection:** Automatically disconnects devices that try to pair when their auto-connect setting is disabled.
*   **Device Pairing:** Quick access to the Windows pairing interface to add new devices.
*   **Persistent Settings:** Your preferences are saved locally and restored every time the app starts.
*   **Clean UI:** A simple, modern WPF interface built for Windows 11.

## 🚀 Getting Started

### Prerequisites
*   Windows 11 Operating System
*   .NET 6.0 Runtime (if running from source)

### Installation
1.  Go to the **Actions** tab in this repository.
2.  Click on the latest workflow run named "Build Bluetooth Sentinel".
3.  Under the **Artifacts** section, download `BluetoothSentinel-Exe`.
4.  Extract the `.exe` file to a folder of your choice.
5.  Run `BluetoothSentinel.exe`.

### Building from Source
If you prefer to build the application yourself:
```bash
git clone https://github.com/cerealicious/BluetoothSentinel.git
cd bluetooth-sentinel
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained true
