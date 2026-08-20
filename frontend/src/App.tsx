import { useEffect, useState } from "react";
import client from "./api/client";
import type { PagedResult, ProductDto } from "./types/product";

function App() {
  const [data, setData] = useState<PagedResult<ProductDto> | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    client
      .get<PagedResult<ProductDto>>('/api/products', {
      params: {page: 1, pageSize: 5},
    })
    .then((res) => setData(res.data))
    .catch((err) => setError(err.message));
  }, []);

  return (
    <div className="min-h-screen bg-slate-50 p-8">
      <h1 className="text-3xl font-bold text-blue-600">Shop</h1>
      {error && <p className="mt-4 text-red-600">Lỗi {error}</p>}
      {data && (
        <p className="mt-4 text-slate-700">
          Tổng: {data.totalCount} sản phẩm - trang {data.page}/{data.totalPages}
        </p>
      )}
      <ul className ="mt-4 space-y-2">
        {data?.items.map((p) => 
        <li key={p.id} className="rounded border border-slate-200 bg-white p-3">
          <span className="font-medium">{p.name}</span>
          <span className="ml-2 text-slate-500">{p.price.toLocaleString('vi-VN')} đ</span>
        </li>)}
      </ul>
    </div>
  )
}
export default App;