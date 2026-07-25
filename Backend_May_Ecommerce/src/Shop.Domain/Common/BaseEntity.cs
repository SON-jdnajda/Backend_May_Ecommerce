namespace Shop.Domain.Common;
public abstract class BaseEntity<TKey>
{
    public TKey Id { get; protected set; } = default!;
    public DateTime CreateAt {get; set;} = DateTime.UtcNow;
    public DateTime UpdateAt {get; set;}
}