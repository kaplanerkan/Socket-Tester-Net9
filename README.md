# Socket-Tester-Net9

A modern **.NET 9 / WPF** rewrite of the classic [SocketTest v3.0](https://sourceforge.net/projects/sockettest/) Java Swing utility — a manual TCP/UDP socket tester for poking at servers, replaying captured payloads, and inspecting raw bytes on the wire.

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **TCP client** with optional **SSL/TLS** and a user-driven cert-trust prompt for self-signed servers
- **TCP server** — single-connection listener for echo / smoke testing
- **UDP** send/receive
- Multiple payload encodings: **UTF-8**, **Windows-1252**, **ASCII**, and a whitespace-tolerant **hex** mode
- Configurable line endings (`none` / `LF` / `CRLF`) appended on send
- Live conversation log with direction-coded entries (sent / received / info / error) and timestamps
- Hand-rolled MVVM — no third-party UI framework dependency

## Requirements

- Windows 10/11 (the GUI is WPF — `net9.0-windows`)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

The `SocketTest.Core` library alone targets `net9.0` and is portable to any platform that runs .NET 9, if you want to reuse it headless.

## Build & run

```powershell
git clone https://github.com/kaplanerkan/Socket-Tester-Net9.git
cd Socket-Tester-Net9

dotnet build SocketTestNet.sln              # build everything
dotnet run --project src/SocketTest.Wpf     # launch the GUI
dotnet build -c Release SocketTestNet.sln   # release build
```

## Project layout

```
SocketTestNet.sln
src/
├── SocketTest.Core/        # UI-agnostic socket sessions (portable, net9.0)
│   ├── TcpClientSession.cs # TCP client + optional SslStream
│   ├── TcpServerSession.cs # Single-connection TCP listener
│   ├── UdpSession.cs       # UDP send/receive loop
│   ├── PayloadEncoding.cs  # UTF-8 / Windows-1252 / ASCII / Hex
│   └── SessionEvents.cs    # SessionEvent record + direction enum
└── SocketTest.Wpf/         # WPF front-end (net9.0-windows)
    ├── MainWindow.xaml     # Tabbed shell
    ├── ViewModels/         # One VM per tab + ConversationLog
    ├── Views/              # TcpClient / TcpServer / Udp / About
    └── Mvvm/               # Hand-rolled INPC base + RelayCommand
```

## Architecture

The split is intentionally thin so the socket logic stays reusable:

- **Core sessions** each own a `CancellationTokenSource` plus a background read/accept loop, and raise progress through a single `event Action<SessionEvent>? EventRaised`. `SessionEvent` is a record carrying direction (`Info` / `Sent` / `Received` / `Error`), text, and timestamp.
- **WPF tab VMs** subscribe to that event and append to a shared `ConversationLog`. Encoding and line-ending settings live on the VM and are pushed onto the session before each send.

When extending the app, add the behavior to Core first behind the `EventRaised` contract, then expose it on the matching tab VM — the WPF project never touches sockets directly.

## Acknowledgements

Inspired by the original **SocketTest v3.0** by Akshathkumar Shetty (2008). This is a clean-room rewrite — no code was copied from the original Java sources.

## License

MIT
