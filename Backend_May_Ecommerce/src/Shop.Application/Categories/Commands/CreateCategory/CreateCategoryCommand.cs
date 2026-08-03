using MediatR;

namespace Shop.Application.Categories.Commands.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    string? Description,
    Guid? ParentId = null
    ) : IRequest<Guid>;
