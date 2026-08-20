namespace Shop.Application.Common.Diagnostics;

/// <summary>
/// Meter names shared between the layer that emits metrics (Infrastructure)
/// and the layer that configures the exporter (API). Keeping them here means
/// neither side hard-codes a magic string the other has to match by hand.
/// </summary>
public static class ShopMeters
{
    public const string Orders = "Shop.Orders";
}
