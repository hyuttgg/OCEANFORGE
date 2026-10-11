# OceanForge Desktop — Known Issues (KNOWN_ISSUES.md)

1. **Wayland Native Rendering trên Linux:**
   - *Mô tả:* Avalonia 12.1 hỗ trợ Wayland native ở trạng thái experimental (`Avalonia.Wayland`).
   - *Workaround:* Mặc định sử dụng XWayland / X11 fallback để đảm bảo độ ổn định cao nhất trên các distro Linux (Ubuntu/Debian/Fedora).

2. **Roblox Executor Rate Limiting:**
   - *Mô tả:* Khi chạy multi-account (10+ accounts song song), các executor có thể gửi telemetry dồn dập vào cùng một giây.
   - *Khắc phục:* Đã thiết lập `x-csharp-dedup-hash` (FNV-1a 32-bit) và `System.Threading.Channels` có bounded capacity để chống nghẽn I/O và drop an toàn đối với các payload trùng lặp.
