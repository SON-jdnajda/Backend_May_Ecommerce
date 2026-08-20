-- Gỡ toàn bộ dữ liệu benchmark, trả DB dev về trạng thái trước khi seed.
-- Chạy sau khi đã đo xong cả 3 mốc.
--
--   docker compose cp perf/cleanup-seed.sql postgres:/tmp/cleanup.sql
--   docker compose exec -e PGPASSWORD=admin postgres psql -h host.docker.internal -p 5432 \
--     -U postgres -d may_fashion_ecommerce -f /tmp/cleanup.sql

BEGIN;

-- Chỉ xoá đúng hàng do seed tạo ra: chúng đều thuộc category 'perf-seed'.
-- Không dùng TRUNCATE - nó sẽ cuốn theo cả sản phẩm thật của bạn.
DELETE FROM "Products"
WHERE "CategoryId" = '00000000-0000-0000-0000-0000000000ff';

DELETE FROM "Categories"
WHERE "Id" = '00000000-0000-0000-0000-0000000000ff';

COMMIT;

VACUUM ANALYZE "Products";

SELECT count(*) AS remaining_products FROM "Products";
