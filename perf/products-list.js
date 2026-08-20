import http from 'k6/http';
import { check } from 'k6';
import { Rate } from 'k6/metrics';

// Đo GET /api/products dưới tải ổn định.
//
// Chạy cùng script này cho cả 3 mốc (A: baseline, B: SQL paging, C: + Redis).
// Chỉ code của app thay đổi giữa các mốc - kịch bản tải phải giữ nguyên,
// nếu không thì các con số không so sánh được với nhau.

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5204';
const PAGE_SIZE = Number(__ENV.PAGE_SIZE || 20);

// Số trang mà VU sẽ bốc ngẫu nhiên.
//   PAGES=1  -> hot key, cache hit ratio ~100% (kịch bản lạc quan nhất)
//   PAGES=50 -> long tail, hit ratio thực tế hơn
// Báo cáo cả hai. Chỉ đưa số của PAGES=1 là đang tự lừa mình.
const PAGES = Number(__ENV.PAGES || 1);

const VUS = Number(__ENV.VUS || 50);

const notOk = new Rate('non_200_responses');

export const options = {
  scenarios: {
    steady: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '20s', target: VUS }, // ramp-up: cho JIT và pool ấm lên
        { duration: '1m', target: VUS },  // giai đoạn đo thật
        { duration: '10s', target: 0 },   // ramp-down
      ],
      gracefulRampDown: '5s',
    },
  },

  // Ngưỡng để k6 tự phán quyết pass/fail. Mốc A nhiều khả năng fail -
  // đó là mục đích: nó cho bạn một con số "trước" có ý nghĩa.
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
  },

  // Không parse body: giữ cho k6 không tự biến thành nút thắt cổ chai.
  discardResponseBodies: true,

  // Dev cert tự ký, chỉ có tác dụng nếu bạn trỏ BASE_URL sang https.
  insecureSkipTLSVerify: true,

  summaryTrendStats: ['avg', 'min', 'med', 'p(95)', 'p(99)', 'max'],
};

export default function () {
  const page = Math.floor(Math.random() * PAGES) + 1;

  const res = http.get(
    `${BASE_URL}/api/products?page=${page}&pageSize=${PAGE_SIZE}`,
    { tags: { name: 'GET /api/products' } },
  );

  const ok = check(res, {
    'status is 200': (r) => r.status === 200,
  });

  notOk.add(!ok);

  // 429 nghĩa là bạn đang bắn qua API Gateway (rate limit 100 req/phút).
  // Benchmark phải trỏ thẳng vào Shop.API, nếu không bạn đang đo rate limiter.
  if (res.status === 429) {
    throw new Error(
      'Nhận 429: BASE_URL đang trỏ vào Gateway. Đổi sang http://localhost:5204.',
    );
  }
}

export function handleSummary(data) {
  const duration = data.metrics.http_req_duration;
  const requests = data.metrics.http_reqs;

  // API chưa chạy -> không có request nào hoàn tất -> metric rỗng.
  // Báo thẳng nguyên nhân thay vì để JS ném lỗi đọc thuộc tính undefined.
  if (!duration || !requests) {
    return {
      stdout:
        '\nKhông có request nào hoàn tất. Kiểm tra API đã chạy ở ' +
        `${BASE_URL} chưa (dotnet run -c Release --launch-profile http).\n\n`,
    };
  }

  const d = duration.values;
  const reqs = requests.values;
  const failed = data.metrics.http_req_failed.values;

  // Mọi request fail -> latency toàn số 0, trông y hệt một kết quả cực tốt.
  // Từ chối in bảng: con số đó không đo được gì và rất dễ bị chép nhầm
  // vào bảng so sánh.
  if (failed.rate >= 1) {
    return {
      stdout:
        `\n100% request FAIL - không có số liệu nào dùng được.\n` +
        `  API tại ${BASE_URL} chưa chạy, hoặc đang trả lỗi.\n\n` +
        `  Kiểm tra theo thứ tự:\n` +
        `    1. Đã seed dữ liệu chưa?\n` +
        `    2. dotnet run -c Release --project src/Shop.API --launch-profile http\n` +
        `    3. curl.exe "${BASE_URL}/api/products?page=1&pageSize=20"\n\n`,
    };
  }

  const line = (label, value) => `  ${label.padEnd(24)} ${value}`;

  const report = [
    '',
    `=== ${__ENV.LABEL || 'run'} | PAGES=${PAGES} VUS=${VUS} ===`,
    line('RPS', reqs.rate.toFixed(1)),
    line('total requests', reqs.count),
    line('p95', `${d['p(95)'].toFixed(1)} ms`),
    line('p99', `${d['p(99)'].toFixed(1)} ms`),
    line('median', `${d.med.toFixed(1)} ms`),
    line('max', `${d.max.toFixed(1)} ms`),
    line('error rate', `${(failed.rate * 100).toFixed(2)} %`),
    '',
    'Chép p95/p99/RPS vào bảng so sánh trong perf/README.md.',
    'Đừng báo cáo avg - nó giấu đuôi phân phối.',
    '',
  ].join('\n');

  return { stdout: report };
}
