# Kế hoạch triển khai End-to-End E-Commerce System

## 1. Tổng quan kiến trúc hiện tại
- **Backend**: Solution .NET 9 (`Backend_Ecommerce.sln`), thiết kế theo Clean Architecture phân tách thành các thư mục con cho Microservices.
  - Hiện tại chỉ có service `Identity` (`Identity.Domain`, `Identity.Application`, `Identity.Infrastructure`, `Identity.API`).
  - Database: PostgreSQL (sử dụng Entity Framework Core).
  - Các công cụ: MediatR (CQRS), FluentValidation, BCrypt, JWT, Serilog.
- **Frontend**: Dự án React 19 (`Frontend_Ecommerce`), build tool Vite, styling bằng Tailwind CSS v4, routing bằng React Router v7.
  - Đã chia cấu trúc thư mục `features`, `shared/components`, `styles`.
- **Hạ tầng**: Có sẵn thư mục `docker` và `docker-compose.yml` (đang trống).

## 2. Những tính năng đã có
- Cấu trúc project backend chuẩn mực.
- Các entity `User`, `Role`, `RefreshToken` và migration EF Core.
- Các endpoint cơ bản của Identity (Login, logic Hash password, JWT Generator).
- Frontend có bộ khung UI cơ bản (Button, ProductCard, Banner, Header, Footer) nhưng chưa kết nối API và thiếu logic.

## 3. Những tính năng còn thiếu
- **Backend API**:
  - Quản lý User (Admin xem danh sách, block, profile).
  - Quản lý Catalog (Category, Brand, Product, Variant, Image, Stock).
  - Giỏ hàng (Cart) và Đặt hàng (Checkout, Order, OrderItem).
  - Mã giảm giá (Coupon) và Đánh giá (Review).
  - Dashboard Admin (Thống kê doanh thu, đơn hàng).
  - API Gateway để gom nhóm các endpoint.
- **Frontend**:
  - Khách hàng: Auth pages, Trang chủ, Danh sách/Chi tiết SP, Giỏ hàng, Checkout, Lịch sử đơn hàng.
  - Admin: Dashboard, Quản lý sản phẩm/đơn hàng/tài khoản.
  - API Client, State Management (Zustand/Redux).
- **Hạ tầng & Test**: Docker Compose hoàn chỉnh, DB Seeding, Unit/Integration Test.

## 4. Các quyết định kỹ thuật
1. **Kiến trúc Backend**: Để cân bằng giữa tính mở rộng và khả năng hoàn thiện nhanh trong môi trường phát triển, tôi sẽ mở rộng solution hiện tại bằng cách tạo thêm một service **`Shop`** (bao gồm `Shop.Domain`, `Shop.Application`, `Shop.Infrastructure`, `Shop.API`). 
   - `Identity.API`: Chuyên trách Authentication, Authorization và quản lý User.
   - `Shop.API`: Xử lý Catalog, Cart, Order, Coupon, Review, Admin Dashboard.
   - **API Gateway (YARP)**: Tạo một project `ApiGateway` để route request từ frontend (VD: `/api/auth` -> Identity, `/api/shop` -> Shop).
2. **Database**: `Shop` service sẽ dùng chung instance PostgreSQL nhưng có DB schema/context riêng (`ShopDbContext`) để đảm bảo tính độc lập của dịch vụ.
3. **Lưu trữ ảnh**: Sử dụng Local Storage cho môi trường development (lưu vào `wwwroot` của `Shop.API`), trả về URL tương đối.
4. **State Management Frontend**: Sử dụng **Zustand** (nhẹ, dễ setup) cho global state (Auth, Cart) và **Axios** để fetch API kèm Interceptor xử lý Refresh Token.
5. **Thanh toán**: Hỗ trợ COD và Mock Payment.

## 5. Kế hoạch triển khai theo thứ tự
- **Giai đoạn 2 (Backend)**:
  - Khởi tạo project `Shop.API` và các lớp (Domain, Application, Infrastructure) tương tự `Identity`.
  - Định nghĩa Entities (Category, Product, Order, Cart...).
  - Triển khai DbContext, Repositories, CQRS Handlers cho các tính năng: Catalog, Cart, Order, Coupon, Admin Stats.
  - Hoàn thiện `Identity.API` (Thêm API quản lý User cho Admin).
  - Khởi tạo `ApiGateway` bằng YARP.
  - Tạo Migration và Seed data.
- **Giai đoạn 3 (Frontend - Khách hàng)**:
  - Setup Axios, Zustand.
  - Ghép API Auth (Login/Register/Refresh).
  - Ghép API Catalog (Trang chủ, chi tiết sản phẩm).
  - Ghép API Cart & Checkout.
- **Giai đoạn 4 (Frontend - Admin)**:
  - Setup layout Admin bảo vệ bằng Role.
  - Xây dựng các trang Quản lý Sản phẩm, Đơn hàng, User và Dashboard.
- **Giai đoạn 5 (Docker & Môi trường)**:
  - Viết `Dockerfile` cho `Identity.API`, `Shop.API`, `ApiGateway`, `Frontend`.
  - Cấu hình `docker-compose.yml` khởi chạy Postgres và các services.
- **Giai đoạn 6 (Kiểm thử)**:
  - Viết Unit Tests cho các logic quan trọng (Order, Cart, Identity) trong thư mục `tests`.
- **Giai đoạn 7 (Tài liệu)**:
  - Viết `README.md` hướng dẫn chạy.
  - Lập `IMPLEMENTATION_REPORT.md` báo cáo kết quả.

## 6. Rủi ro hoặc giả định
- Kiến trúc chia 2 service (`Identity` và `Shop`) đòi hỏi cơ chế lấy thông tin User bên `Shop` service. Giải pháp: Lấy từ JWT Claims để tránh gọi chéo giữa các service hoặc lưu bản sao `UserId` bên `Shop`.
- Sẽ ưu tiên code chạy được, đúng chức năng và dễ đọc hơn là over-engineering.

## User Review Required
Bạn có đồng ý với hướng chia thêm `Shop.API` và `ApiGateway` bằng YARP không? Hay muốn gộp tất cả vào một API Monolith duy nhất để dễ deploy? (Đề xuất: Dùng YARP và `Shop.API` để sát với kiến trúc hiện có).
