# OrderCafe

Ứng dụng ASP.NET MVC 5, sử dụng .NET Framework 4.7.2 và Entity Framework 6 (Database First).

## Cấu trúc repository

| Đường dẫn | Vai trò |
| --- | --- |
| `OrderCafe.slnx` | Solution chính; mở từ thư mục gốc repository. |
| `OrderCafe/OrderCafe.csproj` | Cấu hình web project. |
| `OrderCafe/App_Start/` | Cấu hình route, filter và bundle. |
| `OrderCafe/Controllers/` | Controller xử lý request. |
| `OrderCafe/Models/` | Entity, DbContext, EDMX và template T4 hiện có. |
| `OrderCafe/Views/` | Razor view theo controller và view dùng chung trong `Shared/`. |
| `OrderCafe/Content/` | CSS và hình ảnh. |
| `OrderCafe/Scripts/` | JavaScript phục vụ website. |
| `OrderCafe/Properties/` | Thông tin assembly. |
| `OrderCafe/packages.config` | Danh sách và phiên bản thư viện NuGet. |
| `OrderCafe/Web.config` | Cấu hình ứng dụng và kết nối database. |

## Mở và chuẩn bị project

1. Sử dụng Windows và Visual Studio hỗ trợ định dạng `.slnx`, có workload ASP.NET/web development và .NET Framework 4.7.2 Developer Pack.
2. Mở `OrderCafe.slnx` tại thư mục gốc. Nếu phiên bản Visual Studio chưa hỗ trợ `.slnx`, có thể mở trực tiếp `OrderCafe/OrderCafe.csproj`.
3. Khôi phục NuGet packages trước khi build. Từ thư mục gốc repository, với NuGet CLI trong PATH, chạy:

   ```powershell
   nuget restore OrderCafe/packages.config -PackagesDirectory packages
   ```

   Thư mục `packages/` phải được tạo ở thư mục gốc repository vì project hiện tham chiếu `..\packages\`. Không khôi phục vào `OrderCafe/packages/`.
4. Chuẩn bị SQL Server và database phù hợp với model EDMX. Kết nối hiện có nằm trong `OrderCafe/Web.config`; repository chưa kèm script tạo/seed database.
5. Build và chạy bằng IIS Express trong Visual Studio.

## Quy ước quản lý thư mục

- Giữ nguyên các thư mục MVC và đường dẫn tài nguyên hiện tại để không phải sửa mã nguồn hoặc cấu hình project.
- Chỉ sử dụng solution ở thư mục gốc, tránh tạo thêm solution lồng trong `OrderCafe/`.
- Không commit `.vs/`, `bin/`, `obj/`, `packages/`, `*.csproj.user` hoặc `*.suo`. Đây là cache, kết quả build, dependency có thể khôi phục hoặc thiết lập riêng từng máy.
- Giữ các thư viện CSS/JavaScript trong `Content/` và `Scripts/` vì website đang tham chiếu trực tiếp các file này.
- Nhánh `CodeAI` chỉ dọn repository và bổ sung tài liệu; không sửa các lỗi nghiệp vụ hiện có. Chưa xác minh build/runtime do môi trường thực hiện không có .NET Framework/MSBuild và database.
