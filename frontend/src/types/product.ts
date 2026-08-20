export interface ProductDto {
     id: string;
     name: string;
     description: string | null;
     price: number;
     stockQuantity: number;
     categoryId: string;
}

export interface PagedResult<T> {
     items: T[];
     totalCount: number;
     page: number;
     pageSize: number;
     totalPages: number;
     hasPreviousPage: boolean;
     hasNextPage: boolean;
}