# 🚀 LỘ TRÌNH 14 NGÀY LÀM LẠI DỰ ÁN ECOMMERCE (SENIOR FULL-STACK)

> **Mục tiêu:** Tự tay viết mới 100% dự án Ecommerce (C# ASP.NET Core + EF Core + PostgreSQL + React 19 + Zustand) đạt chuẩn **Production-Ready**, chạy mượt mà, bảo mật cao và chịu tải tốt.

---

## 📅 TUẦN 1: THIẾT KẾ BACKEND MICROSERVICES & CORE BUSINESS LOGIC

### 📍 Ngày 1: Solution Setup & Domain Layer (Clean Architecture)
- **Mục tiêu:** Khởi tạo Solution và thiết kế tầng Core không phụ thuộc vào bất kỳ thư viện ngoài nào.
- **Công việc:**
  - Khởi tạo `.sln` và 4 project (`Shop.Domain`, `Shop.Application`, `Shop.Infrastructure`, `Shop.API`).
  - Cấu hình Dependency Direction đúng nguyên tắc Clean Architecture.
  - Viết các Domain Entities: `Product`, `Category`, `Order`, `OrderItem`, `Cart`, `CartItem`, `Coupon`.
  - Định nghĩa Value Objects và Enum trạng thái (`OrderStatus`).

### 📍 Ngày 2: Persistence Layer & PostgreSQL Concurrency (EF Core)
- **Mục tiêu:** Cấu hình EF Core 9 với PostgreSQL và tích hợp chống bán lố kho (Inventory Overselling).
- **Công việc:**
  - Xây dựng `ShopDbContext` và triển khai `IApplicationDbContext`.
  - Viết `EntityConfiguration` riêng cho từng bảng (Fluent API).
  - Cấu hình **Optimistic Concurrency Token (`xmin`)** cho `Product.StockQuantity`.
  - Thiết lập Repository / DbContext registration với Scoped Lifetime.

### 📍 Ngày 3: CQRS Pattern & MediatR Layer
- **Mục tiêu:** Xây dựng luồng xử lý Command (Ghi) và Query (Đọc) tách biệt.
- **Công việc:**
  - Cấu hình MediatR trong `Shop.Application`.
  - Viết `GetProductsQueryHandler` sử dụng `.AsNoTracking()` tối ưu tốc độ đọc.
  - Viết `CreateOrderCommandHandler` và `AddItemCommandHandler` có xử lý `DbUpdateConcurrencyException`.
  - Tạo các DTOs tối ưu không rò rỉ Domain Model ra ngoài API.

### 📍 Ngày 4: Validation & Global Exception Handling
- **Mục tiêu:** Chuẩn hóa validation và xử lý lỗi tập trung chuẩn RFC 7807.
- **Công việc:**
  - Viết `ValidationBehavior` trong MediatR Pipeline sử dụng **FluentValidation**.
  - Xây dựng `CustomExceptionHandler` triển khai `IExceptionHandler` của .NET 8/9.
  - Định nghĩa chuẩn `ProblemDetails` trả về cho Frontend khi có lỗi (400, 404, 409, 500).

### 📍 Ngày 5: Identity Service & JWT Authentication Flow
- **Mục tiêu:** Xây dựng dịch vụ xác thực người dùng an toàn.
- **Công việc:**
  - Khởi tạo `Identity.API` với ASP.NET Core Identity / JWT.
  - Xây dựng API `Login`, `Register`, `RefreshToken`, `Logout`.
  - Thiết lập cấp **Access Token (JWT ngắn hạn)** và **Refresh Token trong HttpOnly Cookie**.
  - Hash mật khẩu bằng BCrypt / Argon2.

### 📍 Ngày 6: Yarp API Gateway & Rate Limiting Security
- **Mục tiêu:** Xây dựng điểm cổng tập trung và chống tấn công dồn dập (DDoS).
- **Công việc:**
  - Cấu hình **Yarp Reverse Proxy** điều hướng request về `Identity.API` và `Shop.API`.
  - Bổ sung `Microsoft.AspNetCore.RateLimiting` chống spam request.
  - Cấu hình CORS Policy chuẩn cho môi trường Production.

### 📍 Ngày 7: Redis Caching & Standalone Migration CLI
- **Mục tiêu:** Tăng tốc truy vấn và chuẩn hóa quy trình Deploy Database.
- **Công việc:**
  - Cấu hình **Redis Container** tích hợp `IDistributedCache`.
  - Áp dụng **Cache-Aside Pattern** cho `GetProductsQuery` kèm cơ chế Invalidate Cache khi Admin cập nhật sản phẩm.
  - Tách lệnh `Database.Migrate()` ra khỏi API Startup thành tiến trình CLI/Job độc lập.

---

## 📅 TUẦN 2: FRONTEND REACT 19, STATE MANAGEMENT & INTEGRATION

### 📍 Ngày 8: Setup Frontend React + Vite + TailwindCSS + Axios Client
- **Mục tiêu:** Khởi tạo bộ khung Frontend và cấu hình HTTP Client chuẩn.
- **Công việc:**
  - Khởi tạo React 19 + TypeScript với Vite và TailwindCSS.
  - Xây dựng Axios Instance (`client.ts`) với `baseURL` và `withCredentials: true`.
  - Viết Axios Request Interceptor để đính kèm `Bearer AccessToken`.

### 📍 Ngày 9: Token Refresh Interceptor & Zustand Auth Store
- **Mục tiêu:** Quản lý trạng thái đăng nhập và luồng tự động gia hạn token.
- **Công việc:**
  - Viết Axios Response Interceptor xử lý lỗi 401 với thuật toán gom Promise (`refreshRequest ??= ...`).
  - Xây dựng `useAuthStore` bằng Zustand để quản lý `user`, `login()`, `logout()`, `initialize()`.
  - Tạo HOC / Component `ProtectedRoute` bảo vệ các route yêu cầu đăng nhập.

### 📍 Ngày 10: Product Catalog & Filtering UI
- **Mục tiêu:** Hiển thị danh sách sản phẩm với trải nghiệm người dùng cao cấp.
- **Công việc:**
  - Xây dựng trang `ProductList` với bộ lọc danh mục, tìm kiếm và phân trang.
  - Viết `ProductCard` với hiệu ứng Hover, Skeleton Loading khi đang tải dữ liệu.
  - Xây dựng trang `ProductDetail` hiển thị chi tiết thông số, tồn kho và nút thêm giỏ hàng.

### 📍 Ngày 11: Shopping Cart & Checkout Flow
- **Mục tiêu:** Xây dựng luồng giỏ hàng và đặt hàng mượt mà.
- **Công việc:**
  - Xây dựng `useCartStore` quản lý thêm/sửa/xóa sản phẩm trong giỏ hàng.
  - Áp dụng **Optimistic UI Update** giúp trải nghiệm bấm thêm vào giỏ tức thì.
  - Xây dựng trang `Checkout` nhập địa chỉ, mã giảm giá và gửi `CreateOrderCommand`.

### 📍 Ngày 12: Order Management & User Dashboard
- **Mục tiêu:** Cho phép người dùng theo dõi trạng thái đơn hàng.
- **Công việc:**
  - Xây dựng trang `MyOrders` hiển thị lịch sử mua hàng.
  - Xây dựng Component `OrderStatusBadge` hiển thị trạng thái (Pending, Processing, Completed, Cancelled).
  - Cho phép người dùng hủy đơn hàng khi đơn chưa được duyệt.

### 📍 Ngày 13: Global Error Boundary & Performance Optimization
- **Mục tiêu:** Tăng độ bền ứng dụng và tối ưu tốc độ tải trang.
- **Công việc:**
  - Xây dựng `ErrorBoundary` hứng crash giao diện React.
  - Tích hợp hệ thống Toast Notification thông báo lỗi từ Backend.
  - Tối ưu Code Splitting với `React.lazy()` và `Suspense` cho các trang chính.

### 📍 Ngày 14: Docker Orchestration, E2E Verification & Review
- **Mục tiêu:** Đóng gói toàn bộ ứng dụng và kiểm thử hoàn chỉnh.
- **Công việc:**
  - Xây dựng file `docker-compose.yml` chạy full chuỗi: PostgreSQL, Redis, Identity.API, Shop.API, ApiGateway, React Frontend.
  - Thử nghiệm kịch bản tải cao và mua hàng đồng thời (Concurrency Test).
  - Tổng kết toàn bộ dự án, review kiến trúc và nghiệm thu!

---
> 💡 *Mỗi ngày bạn sẽ dành 1.5 - 2 tiếng tự gõ code. Tôi sẽ đồng hành cùng bạn từng câu lệnh, từng class một!*
