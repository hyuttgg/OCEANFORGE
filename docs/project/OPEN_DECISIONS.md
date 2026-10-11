# OceanForge Desktop — Open Decisions (OPEN_DECISIONS.md)

1. **MySQL Provider EF Core 10:**
   - *Vấn đề:* Pomelo official provider đang ở EF Core 9. Fork `Microting.EntityFrameworkCore.MySql` có bản EF Core 10 nhưng cần theo dõi độ ổn định dài hạn.
   - *Phương án mặc định:* Dùng `MySqlConnector` trực tiếp cho hot paths hoặc fork đã test trong container.

2. **Cổng lắng nghe của Local HTTP Bridge cho Roblox Lua Client:**
   - *Vấn đề:* Script `sender copy.lua` mặc định gửi về `https://quan-ly-acc-viet-nam.onrender.com` hoặc biến môi trường `OceanForgeCSharpBridgeUrl`.
   - *Phương án mặc định:* Desktop App mở local port (ví dụ `http://localhost:5088`) và cung cấp một nút bấm "Copy Local Bridge Script" để user dán vào Roblox executor.
