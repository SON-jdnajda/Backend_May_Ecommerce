-- Seed dữ liệu cho benchmark. KHÔNG chạy trên DB thật.
--
-- Với 5 sản phẩm thì bug in-memory paging không lộ ra và mốc A ~ mốc B.
-- Cần đủ số dòng để việc kéo cả bảng về RAM thực sự đau.
--
--   docker compose exec postgres psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -f /dev/stdin < perf/seed-products.sql
--
-- hoặc mở psql rồi \i, hoặc dán trực tiếp.

\set row_count 50000

BEGIN;

-- Category cho hàng seed. Slug có unique index nên ON CONFLICT bám vào đó.
INSERT INTO "Categories" ("Id", "Name", "Slug", "Description", "ParentId", "CreatedAt")
VALUES (
  '00000000-0000-0000-0000-0000000000ff',
  'Perf Seed',
  'perf-seed',
  'Danh mục rác dùng cho benchmark. Xoá sau khi đo xong.',
  NULL,
  now()
)
ON CONFLICT ("Slug") DO NOTHING;

INSERT INTO "Products"
  ("Id", "Name", "Description", "Price", "StockQuantity", "CategoryId", "Version", "CreatedAt", "UpdatedAt")
SELECT
  gen_random_uuid(),
  'Perf Product ' || i,
  'Seeded row ' || i || ' - ' || repeat('x', 200),  -- description có độ dài thật, để payload không nhỏ giả tạo
  (random() * 1000)::numeric(18, 2),
  (random() * 100)::int,
  '00000000-0000-0000-0000-0000000000ff',
  0,
  now() - (i || ' seconds')::interval,              -- CreatedAt phân tán, không dồn 1 mốc
  NULL
FROM generate_series(1, :row_count) AS i;

COMMIT;

-- Postgres cần thống kê mới thì planner mới chọn đúng kế hoạch.
-- Bỏ bước này là bạn đang benchmark một planner đang đoán mò.
ANALYZE "Products";

SELECT count(*) AS total_products FROM "Products";
