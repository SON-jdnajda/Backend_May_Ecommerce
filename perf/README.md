# Benchmark `GET /api/products`

Đo tác động của từng thay đổi lên hiệu năng, tách bạch **lợi ích của sửa query**
khỏi **lợi ích của cache**.

```
perf/
  products-list.js     kịch bản k6 (không sửa giữa các mốc - đó là điều kiện để so sánh được)
  run.ps1              chạy đo, có kiểm tra điều kiện trước
  compare.ps1          đọc perf/out/*.json và in bảng so sánh
  seed-products.sql    seed 50.000 sản phẩm
  cleanup-seed.sql     gỡ dữ liệu seed, trả DB về sạch
  out/                 kết quả JSON (gitignored)
```

---

## Quy trình mỗi lần đo

```powershell
# Terminal 1 - API, để chạy suốt
cd Backend_May_Ecommerce
dotnet run -c Release --project src/Shop.API --launch-profile http

# Terminal 2
cd D:\Learn\Ecommerce
.\perf\run.ps1 -Check              # kiểm tra k6 + API + dữ liệu
.\perf\run.ps1 -Label C -Repeat 3  # đo
.\perf\compare.ps1 -Baseline A     # xem bảng
```

`run.ps1` tự lo warm-up, đặt tên file, và chặn khi môi trường chưa sẵn sàng.
`compare.ps1` tự đọc kết quả — **không chép số bằng tay**, đó là cách nhanh nhất
để gán nhầm số của mốc này sang mốc khác.

### Tham số hay dùng

| Lệnh | Dùng khi |
| --- | --- |
| `.\perf\run.ps1 -Check` | kiểm tra môi trường, không đo |
| `.\perf\run.ps1 -Label C` | đo cả hot + tail, 1 lần |
| `.\perf\run.ps1 -Label C -Repeat 3` | đo 3 lần để biết sàn nhiễu |
| `.\perf\run.ps1 -Label C -Pages 1` | chỉ hot, nhanh gấp đôi |
| `.\perf\run.ps1 -Label C -Vus 100` | thử mức tải cao hơn |
| `.\perf\compare.ps1 -Baseline B` | so mọi mốc với B |

**hot** (`PAGES=1`): mọi VU gọi `page=1` → cache hit ratio ~100%, kịch bản lạc quan nhất.
**tail** (`PAGES=50`): VU rải đều 50 trang → hit ratio thực tế hơn. Báo cáo cả hai.

---

## Chuẩn bị (một lần)

```powershell
docker compose up -d postgres redis
```

Seed 50.000 sản phẩm vào PostgreSQL native `:5432`:

```powershell
docker compose cp perf/seed-products.sql postgres:/tmp/seed.sql
docker compose exec -e PGPASSWORD=admin postgres psql -h host.docker.internal -p 5432 -U postgres -d may_fashion_ecommerce -f /tmp/seed.sql
```

> Mượn `psql` trong container để nói chuyện với Postgres trên host, vì máy không
> cài psql. Phải thấy `total_products | 50000`.

Đo xong hết thì dọn bằng `perf/cleanup-seed.sql` (lệnh ghi trong file).

---

## Kỷ luật khi đo

Hai lỗi đã thực sự xảy ra trong dự án này, đừng lặp lại:

1. **Đo các mốc cách nhau nhiều giờ.** Mốc B đo lúc 10h sáng cho 1648 RPS; đúng
   code đó đo lúc 21h30 cho 2493 RPS — **lệch 51% mà không đổi một dòng nào**.
   Buffer pool của Postgres, OS page cache, tải nền đều đã khác.
   → Các mốc cần so sánh phải đo **liền nhau trong cùng một phiên**.

2. **Đo trước khi restart API.** Sửa code mà quên `dotnet build` + restart thì
   bạn đo lại đúng mốc cũ dưới một cái nhãn mới.

Ngoài ra:

- Chạy `-c Release`, không phải `Debug`.
- Bắn thẳng vào `:5204`, **không qua Gateway `:5288`** — rate limit 100 req/phút
  sẽ biến bài test thành bài đo 429. `products-list.js` sẽ throw nếu gặp 429.
- Dùng `-Repeat 3` cho mốc quan trọng. Nếu chênh lệch giữa hai mốc nhỏ hơn độ
  lệch giữa các lần lặp thì **không kết luận được gì**.
- Đóng Chrome / IDE thừa — k6, API và Postgres đang tranh CPU cùng một máy.

---

## Kết quả

Môi trường: 50.000 sản phẩm, PostgreSQL native `:5432`, API `Release` trên
`:5204`, 50 VU, cùng một máy. Xem số mới nhất bằng `.\perf\compare.ps1`.

| Mốc | Code | Ghi chú |
| --- | --- | --- |
| **A** | `GetAllAsync()` rồi `.Skip().Take()` trong RAM | baseline |
| **B** | `GetPagedAsync()` — phân trang bằng SQL | |
| **B-later** | y hệt B, đo lại sau 11 tiếng | giữ lại làm bằng chứng về sàn nhiễu |
| **C** | B + Cache-Aside qua Redis | *chưa có* |

### Kịch bản `hot` (PAGES=1)

| Mốc | RPS | p95 (ms) | p99 (ms) | median (ms) | lỗi |
| --- | --- | --- | --- | --- | --- |
| A | 34.9 | 2275.5 | 2802.2 | 1168.9 | 0% |
| B | 1648.7 | 53.8 | 76.5 | 22.5 | 0% |
| B-later | 2493.1 | 39.3 | 63.2 | 13.1 | 0% |

### Kịch bản `tail` (PAGES=50)

| Mốc | RPS | p95 (ms) | p99 (ms) | median (ms) | lỗi |
| --- | --- | --- | --- | --- | --- |
| A | 37.1 | 2135.1 | 2563.7 | 1080.8 | 0% |
| B | 1711.2 | 51.5 | 70.1 | 20.9 | 0% |
| B-later | 2333.9 | 42.2 | 65.9 | 13.8 | 0% |

### Đọc kết quả

- **A → B: throughput ×47, p95 giảm 97.6%**, chỉ bằng cách đẩy phân trang xuống
  SQL. Không thêm hạ tầng, không thêm dependency.
- Ở mốc A, tăng VU 10→50 không tăng RPS (37→35) mà thổi p95 từ 375ms lên 2275ms:
  throughput đã bão hoà, thêm concurrency chỉ làm dài hàng đợi (Little's Law,
  `L = λ × W`). Ở mốc B, 10→50 VU vẫn tăng RPS 1279→1649 — còn dư địa.
- Ở mốc A, `hot` và `tail` bằng nhau vì **số trang không liên quan**: mọi request
  đều nạp cả 50.000 dòng. Đó là bằng chứng đo được cho bug in-memory paging.
- **error rate 0% ở cả ba mốc.** Mốc A không sập, chỉ chậm — kiểu hỏng không
  alert nào bắt được, người dùng chỉ lặng lẽ chờ 2 giây.

---

## Giới hạn — nói ra trước khi bị hỏi

> Số đo trên localhost: không có network latency, load generator và server tranh
> CPU cùng một máy, một instance Postgres duy nhất, dataset đồng nhất. Con số
> tuyệt đối không dùng cho capacity planning. Nó dùng để **so sánh tương đối**
> giữa các phiên bản code, và để xác nhận thay đổi thật sự hạ tải xuống DB chứ
> không chỉ "cảm giác nhanh hơn".

Chủ động nêu giới hạn là dấu hiệu của người đã thực sự đo, không phải người chép số.
