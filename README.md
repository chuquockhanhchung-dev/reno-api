# RENO CAFÉ API (ASP.NET Core 9 + SQLite)

Backend cho app Công thức & Vận hành theo ca. **Không cần cài SQL Server, không cần lệnh migration** — chạy lần đầu tự tạo file `reno.db` và nạp sẵn: 4 tài khoản, 40 việc checklist (từ Vận_hành.xlsx), 46 NVL + 33 món (từ công_thức_pha_chế.xlsx).

## Chạy trên máy Windows

1. Cài **.NET 9 SDK**: https://dotnet.microsoft.com/download/dotnet/9.0 (chọn SDK, không phải Runtime)
2. Mở CMD tại thư mục `RenoApi`:

```cmd
dotnet run
```

Lần đầu nó tải thư viện (~1 phút). Thấy dòng `Now listening on: http://0.0.0.0:5088` là xong.

3. Mở trình duyệt: **http://localhost:5088** → tự nhảy vào Swagger (trang test API).

Thử ngay: mở `POST /api/auth/login` → Try it out → nhập:
```json
{ "username": "truong", "password": "123456" }
```
→ Execute → nhận về `accessToken`. Bấm nút **Authorize** (ổ khóa góc phải trên), dán `Bearer <token>` → giờ gọi được mọi API khác.

## Tài khoản seed (⚠️ đổi mật khẩu trước khi dùng thật)

| Username | Mật khẩu | Vai trò |
|---|---|---|
| truong | 123456 | Manager (Cửa hàng trưởng) |
| phache | 123456 | Barista (Pha chế – Thu ngân) |
| phucvu, phucvu2 | 123456 | Service (Phục vụ) |

Đổi mật khẩu: `PUT /api/users/{id}` với `{"password": "mật-khẩu-mới"}` (đăng nhập bằng truong).

## Danh sách API

| Method | Đường dẫn | Ai gọi | Việc |
|---|---|---|---|
| POST | /api/auth/login | Tất cả | Đăng nhập → token |
| GET/POST/PUT | /api/users | Manager | Quản lý tài khoản nhân viên |
| GET | /api/tasks | Tất cả | Checklist (NV chỉ thấy việc vị trí mình) |
| POST/PUT/DELETE | /api/tasks | Manager | Thêm/sửa/ẩn việc |
| POST | /api/sessions/open | NV | Mở ca `{date?, shift}` → trả session + checklist |
| GET | /api/sessions?from&to | Manager: tất cả · NV: của mình | Danh sách ca |
| POST | /api/sessions/{id}/checkin · /checkout | NV (chủ ca) | Chấm công vào / ra ca |
| PUT | /api/sessions/{id}/tasks/{tid} | NV | Tick việc `{done}` — thiếu ảnh trả lỗi `PHOTO_REQUIRED` |
| POST | /api/sessions/{id}/tasks/{tid}/photo | NV | Upload ảnh minh chứng (form-data, field `file`) |
| PUT | /api/sessions/{id}/tasks/{tid}/review | Manager | Duyệt `{review: "pass"/"fail"/null, note}` |
| PUT | /api/sessions/{id}/manager-note | Manager | Nhận xét chung cuối ca |
| GET | /api/ingredients | Tất cả | NV: chỉ tên+đơn vị · Manager: đủ giá + cost/đơn vị |
| POST/PUT/DELETE | /api/ingredients | Manager | Sửa giá NVL → cost mọi món tự đổi |
| GET | /api/recipes | Tất cả | NV: định lượng + cách làm · Manager: + cost dòng, tổng cost |
| POST/PUT/DELETE | /api/recipes | Manager | Thêm/sửa/xóa món |
| GET | /api/reports/summary?days=7 | Manager | Báo cáo: ca, bỏ sót, không đạt, xếp hạng NV |

Quy tắc nghiệp vụ đã cài sẵn trong API: chưa check-in thì không tick được việc; đã check-out thì checklist khóa; việc `requiresPhoto` bắt buộc có ảnh mới tick được; nhân viên không bao giờ nhìn thấy giá cost.

## Nối với app Flutter

Điện thoại/app web phải gọi được máy chạy API:
- Chạy thử trong quán: lấy IP LAN của máy (`ipconfig` → IPv4, VD `192.168.1.10`) → baseUrl = `http://192.168.1.10:5088/api`. Nhớ cho phép port 5088 qua Windows Firewall (lần đầu chạy nó hỏi → Allow).
- Enum gửi/nhận dạng chữ thường khớp tên enum Dart: `manager/barista/service`, `morning/afternoon/evening`, `opening/closing`, `pass/fail`. Riêng ưu tiên: API trả `required` ↔ Dart là `required_`.
- Ảnh: API trả `photoPath` dạng `/photos/xxx.jpg` → hiển thị bằng `http://IP:5088/photos/xxx.jpg`.
- Bước cuối là viết nốt `ApiRepository` trong app Flutter (đã có sẵn khung Dio) rồi đổi 1 dòng trong `providers.dart` — phần này làm ở lượt kế tiếp.

## Ghi chú vận hành

- **Dữ liệu** nằm ở file `reno.db` cùng thư mục + ảnh trong `wwwroot/photos/` — backup = copy 2 thứ này.
- **Đổi khóa JWT** trong `appsettings.json` (`Jwt:Key`) thành chuỗi bất kỳ >32 ký tự trước khi dùng thật.
- App web Firebase (https) gọi API http trong LAN sẽ bị trình duyệt chặn (mixed content) — dùng bản **APK Android** cho nhân viên, hoặc đưa API lên server có HTTPS. Muốn cả quán + nhiều cửa hàng dùng chung qua Internet thì thuê VPS (~100k/tháng) chạy `docker build -t reno . && docker run -p 5088:5088 reno`, mình hướng dẫn chi tiết khi bạn cần.
- Sau này muốn chuyển SQL Server: thêm package `Microsoft.EntityFrameworkCore.SqlServer`, đổi `UseSqlite` → `UseSqlServer` trong `Program.cs` là xong.
