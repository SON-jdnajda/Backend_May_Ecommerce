namespace Shop.Application.Common.Caching;

// Reader and invalidator must agree on the exact same string. A typo in either
// place silently stops invalidation, so both sides read it from here.
public static class CacheScopes
{
    public const string Products = "products";
}
