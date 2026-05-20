# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`net9/` is a from-scratch **.NET 9 / WPF** rewrite of the legacy SocketTest v3.0 Java Swing tool that lives in the parent directory (`../SocketTest.jar`). It targets the same use case — a manual TCP client / TCP server / UDP socket tester — but is the only buildable source tree here.

The parent `../CLAUDE.md` documents the payload files (`19_*`, `22_*`, `result`, etc.) and the `;!;` / `;SON` wire protocol used by the target POS server. Those payloads are the primary test fixtures fed into this app; respect that file format when adding features that parse or generate them.

## Build / run

Windows + .NET 9 SDK required. WPF means **the GUI project only builds and runs on Windows** (`net9.0-windows`); `SocketTest.Core` alone is `net9.0` and is portable.

```powershell
dotnet build SocketTestNet.sln              # build both projects
dotnet build src/SocketTest.Core            # core only (portable)
dotnet run --project src/SocketTest.Wpf     # launch the GUI
dotnet build -c Release SocketTestNet.sln   # release build
```

There is no test project and no lint config — `dotnet build` is the only verification step. If you add tests, create a new `tests/` folder and add a project reference; don't shoehorn tests into `SocketTest.Core`.

## Architecture

Two-project layout, intentionally thin:

- **`src/SocketTest.Core`** — UI-agnostic socket sessions. Three sealed `*Session` classes (`TcpClientSession`, `TcpServerSession`, `UdpSession`), each `IAsyncDisposable`, each owning its own `CancellationTokenSource` + background read/accept loop. They raise progress through one `event Action<SessionEvent>? EventRaised` — `SessionEvent` is a record carrying `Direction` (Info / Sent / Received / Error), text, and timestamp. That event is the single integration seam with the UI.
- **`src/SocketTest.Wpf`** — WPF front-end. `MainViewModel` owns one tab VM per session type (`TcpClientTabViewModel`, `TcpServerTabViewModel`, `UdpTabViewModel`). Each tab VM wraps a Core session, subscribes to `EventRaised`, and appends to a shared `ConversationLog` that the views render. MVVM plumbing is hand-rolled — no framework dependency — via `Mvvm/ViewModelBase.cs` (INPC) and `Mvvm/RelayCommand.cs`.

Important cross-cutting pieces in Core:

- **`PayloadEncoding`** (`Utf8` / `Windows1252` / `Ascii` / `Hex`) and **`LineEnding`** (`None` / `Lf` / `CrLf`) are set as properties on each session and applied on every send/receive. Hex mode decodes/encodes a whitespace-tolerant hex dump. `Windows1252` is registered via `CodePagesEncodingProvider` in a static ctor — keep the `System.Text.Encoding.CodePages` package reference.
- **SSL/TLS for the TCP client** goes through `SslStream` with a `CertificateTrustPrompt` callback hoisted up to the VM so the UI can ask the user whether to trust a bad cert. Don't silently `return true` from `ValidateCertificate` — the user-prompt flow is the whole point.

When adding a new session feature, do it in Core first behind the `EventRaised` contract, then expose it on the matching tab VM. Don't reach into sockets from the WPF project.

## Conventions specific to this codebase

- `Nullable` and `ImplicitUsings` are both enabled on both projects — write null-aware code and rely on global usings rather than per-file `using System;` clutter.
- Sessions are single-connection by design (`TcpServerSession` accepts one client at a time and drops new ones until the current disconnects). If the user asks for multi-client support, that's a real architectural change — flag it, don't just bolt on a list.
- Read buffers are fixed 8 KiB. The protocol in `../` is delimiter-based (`;SON`), so a single `ReadAsync` may return a partial record — current code surfaces raw chunks to the UI without reassembly. If you add framing, do it in Core and keep the raw-bytes view available too.
- `Raise(...)` swallows handler exceptions on purpose (UI thread marshalling failures shouldn't kill the read loop). Don't "fix" that without a replacement strategy.
