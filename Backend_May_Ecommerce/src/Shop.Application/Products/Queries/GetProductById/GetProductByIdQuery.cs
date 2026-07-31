using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shop.Application.Products.Queries.GetProductById
{
    public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;
}
