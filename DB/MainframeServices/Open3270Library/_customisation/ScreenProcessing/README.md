# TerminalScreenDb

A high-performance, double-buffered **25×80 terminal screen manager** for .NET 8+, designed for TN3270-style host emulation or text-based screen automation.

---

## ✨ Features

| Capability | Description |
|-------------|--------------|
| **Double-buffered rendering** | Writers paint to a back buffer, then atomically swap — readers never see a half-drawn frame. |
| **Zero read blocking** | Reads are lock-free, always working on the latest committed front buffer. |
| **Async & awaitable** | Await screen updates (`WaitForRefreshesAsync`) or debounce stability (`WaitUntilStableAsync`). |
| **Change events** | `ScreenChanged` and `ScreenChangedSimple` fire after each commit. |
| **Stability detection** | `IsStable` / `IsUnstable` properties and configurable `StableAfter` debounce window (default 200 ms). |
| **TN3270E adapter** | Optional parser that consumes telnet-stripped 3270 data streams (`SBA`, `TEXT`, `EUA`, etc.) and updates the screen. |
| **Configurable screen size** | Default 25×80, but any `rows×cols` can be used. |
| **Thread-safe** | One writer at a time; any number of concurrent readers. |

---

## 🚀 Quick Start

```csharp
var screen = new Terminal.TerminalScreenDb(25, 80)
{
    StableAfter = TimeSpan.FromMilliseconds(250)
};

// Subscribe to updates
screen.ScreenChangedSimple += v => Console.WriteLine($"Screen v{v}");
screen.ScreenChanged += (_, e) => Console.WriteLine($"Screen v{e.Version} at {e.UtcCommittedAt:o}");

// Paint a frame atomically
await using (var build = await screen.BeginBuildAsync(clearBackBuffer: true))
{
    build.WriteSpan1(1, 1, "WELCOME TO HOST");
    build.WriteField1(5, 10, 20, "ACCOUNT: 123456");
} // Commit() on dispose performs O(1) buffer swap

// Read snapshot (lock-free)
string header = screen.ReadRun1String(1, 1, 80);

// Wait for a future commit
await screen.WaitForRefreshesAsync(2);

// Wait until the screen is stable (no updates for StableAfter)
await screen.WaitUntilStableAsync();
