# 📓 TÀI LIỆU GHI CHÉP & NHẬT KÝ HỌC TẬP (SENIOR FULL-STACK)

> **Dự án:** E-Commerce Microservices & React 19  
> **Học viên:** Senior Full-Stack Track  
> **Mentor:** Antigravity (Google DeepMind Team)

---

## 📌 CHƯƠNG 1: BACKEND ARCHITECTURE & ASP.NET CORE LIFETIMES

### 1.1 Dependency Injection Service Lifetimes
- **Transient:** Tạo mới mỗi lần được inject/request.
- **Scoped:** Tạo 1 instance cho mỗi HTTP Request (hoặc trong 1 `CreateScope()`).
- **Singleton:** Tạo 1 lần duy nhất và sống suốt đời sống ứng dụng.

### 1.2 Lỗi "Captive Dependency" (Phụ thuộc bị bắt cóc)
- **Định nghĩa:** Xảy ra khi một đối tượng có lifetime dài (Singleton) tiêm (inject) trực tiếp một đối tượng có lifetime ngắn (Scoped).
- **Hậu quả với DbContext:**
  - `DbContext` KHÔNG Thread-Safe -> Văng lỗi `InvalidOperationException` khi có nhiều request đồng thời.
  - Phình to bộ nhớ RAM (Memory Leak) và giữ dữ liệu cũ (Stale Data) do Change Tracker không được giải phóng.
- **Giải pháp:** Sử dụng `app.Services.CreateScope()` để tạo scope tạm thời khi thực thi các tác vụ khởi chạy app (như Migration/Seeding).

### 1.3 Quản lý Database Migration trong Microservices Production
- **Rủi ro:** Gọi `Database.MigrateAsync()` khi startup ở môi trường Multi-Instance/Container sẽ gây ra **DDL Table Lock**, **Lock Timeout**, hoặc **CrashLoopBackOff** làm sập dịch vụ.
- **Chuẩn Production:** Tách Migration ra thành một **CI/CD Pipeline Job** hoặc **InitContainer** riêng biệt trước khi rollout code mới.

---

## 📌 CHƯƠNG 2: ENTITY FRAMEWORK CORE & HIỆU NĂNG TRUY VẤN

### 2.1 Cơ chế Change Tracker & `AsNoTracking()`
- **Mặc định:** EF Core tạo một bản sao (Snapshot) trên bộ nhớ RAM C# để theo dõi thay đổi entity.
- **Dùng `AsNoTracking()` khi nào:** Dùng cho 100% các câu truy vấn **Read-Only (GET API)**.
- **Lợi ích:**
  - Giảm 30% - 50% RAM tiêu thụ.
  - Tăng tốc CPU (bỏ qua bước snapshot & identity map).
  - Giảm áp lực cho Garbage Collector (GC), tránh OOM Container.
- **Không dùng khi nào:** Các tác vụ UPDATE/DELETE mà bạn cần gọi `SaveChangesAsync()`.

### 2.2 Tối ưu LINQ: `.Include()` vs `.Select()`
- Khi đã dùng `.Select(...)` để chiếu dữ liệu ra DTO, EF Core tự động sinh câu lệnh SQL `JOIN` lấy đúng các cột cần thiết.
- Việc viết cả `.Include(...)` ở trước `.Select(...)` là **mã thừa (redundant code)**.

---

## 📌 CHƯƠNG 3: DOMAIN LOGIC & CONCURRENCY CONTROL

### 3.1 Bài toán Bán lố kho (Inventory Overselling)
- Xảy ra khi 2 người dùng cùng mua 1 sản phẩm cuối cùng tại cùng một thời điểm (**Race Condition / Lost Update**).

### 3.2 So sánh Optimistic vs Pessimistic Concurrency

| Tiêu chí | Optimistic Concurrency (Khóa lạc quan) | Pessimistic Concurrency (Khóa bi quan) |
| :--- | :--- | :--- |
| **Cơ chế** | Kiểm tra token phiên bản (`xmin` / `RowVersion`) lúc Update | Khóa cứng dòng dữ liệu (`SELECT FOR UPDATE`) lúc Read |
| **Hiệu năng** | **Cực cao (High Throughput)**, không khóa DB | **Thấp (Low Throughput)**, dễ nghẽn DB connection |
| **Phù hợp** | Web Ecommerce đọc nhiều hơn ghi (99% Read, 1% Write) | Hệ thống ngân hàng, giữ vé xem phim thời gian thực |
| **Xử lý lỗi** | Quăng `DbUpdateConcurrencyException` để hủy transaction | Đứng chờ người trước giải phóng Lock |

---

## 📌 CHƯƠNG 4: BẢO MẬT FULL-STACK & JWT AUTHENTICATION

### 4.1 Cơ chế Xác thực JWT Stateless
- Backend kiểm tra chữ ký (`Signature`) bằng **Secret Key** mà không cần query Database.
- Tốc độ cực nhanh nhưng khó Revoke tức thì nếu Token bị lộ (cần dùng Redis Blacklist).

### 4.2 Axios Silent Refresh Token (`refreshRequest ??= ...`)
- **Vấn đề:** Khi 10 API đồng thời nhận lỗi `401 Unauthorized`, nếu không gom request sẽ bị spam API refresh token dồn dập -> Dính cơ chế Refresh Token Rotation và bị Logout.
- **Giải pháp:** Sử dụng **Promise Deduplication (Single Promise)** bằng toán tử `??=` để 10 request cùng chờ 1 lượt refresh duy nhất.

### 4.3 `HttpOnly Cookie` vs `localStorage`
- **`localStorage`:** Dễ bị tấn công **XSS** cướp Token bằng JavaScript độc hại.
- **`HttpOnly Cookie`:** Chặn 100% JavaScript truy cập (`document.cookie`), trình duyệt tự gửi đính kèm request -> Chống cướp Token an toàn tuyệt đối.

---

## 📝 NHẬT KÝ BÀI TẬP VỀ NHÀ & NHỮNG LỖI CẦN TRÁNH

### Lỗi đã ghi nhận (Tracked Errors):
1. ❌ **Lỗi #1:** Nhầm lẫn Snapshot của EF Core nằm dưới DB (Đúng: Nằm trên RAM C#).
2. ❌ **Lỗi #2:** Nhầm lẫn Caching (RAM) và DB Transaction Locking.
3. ❌ **Lỗi #3:** Hiểu nhầm Optimistic Concurrency vẫn bị bug overselling (Đúng: EF Core quăng ngoại lệ để hủy transaction, bảo vệ kho).

---

> 💡 *Hãy tiếp tục cập nhật file này sau mỗi ngày học!*
