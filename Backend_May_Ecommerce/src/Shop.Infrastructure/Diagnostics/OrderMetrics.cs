using System.Diagnostics.Metrics;
using Shop.Application.Common.Diagnostics;

namespace Shop.Infrastructure.Diagnostics;

/// <summary>
/// Owns the <see cref="Meter"/> for order telemetry. Registered as a singleton:
/// a Meter is meant to live for the process, and creating one per request would
/// leak instruments and duplicate time series.
/// </summary>
public sealed class OrderMetrics : IOrderMetrics, IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _ordersPlaced;
    private readonly Counter<long> _ordersCancelled;
    private readonly Histogram<double> _orderValue;
    private readonly Histogram<int> _itemsPerOrder;

    public OrderMetrics()
    {
        _meter = new Meter(ShopMeters.Orders, "1.0.0");

        _ordersPlaced = _meter.CreateCounter<long>(
            "shop.orders.placed",
            unit: "{order}",
            description: "Number of orders successfully persisted.");

        _ordersCancelled = _meter.CreateCounter<long>(
            "shop.orders.cancelled",
            unit: "{order}",
            description: "Number of orders cancelled after being placed.");

        _orderValue = _meter.CreateHistogram<double>(
            "shop.orders.value",
            unit: "VND",
            description: "Monetary value of each placed order.");

        _itemsPerOrder = _meter.CreateHistogram<int>(
            "shop.orders.items",
            unit: "{item}",
            description: "Distinct line items per placed order.");
    }

    public void OrderPlaced(int itemCount, decimal totalAmount)
    {
        _ordersPlaced.Add(1);
        _itemsPerOrder.Record(itemCount);

        // decimal is exact but has no metrics overload; the precision loss is
        // irrelevant for a latency/value distribution and the range is safe.
        _orderValue.Record((double)totalAmount);
    }

    public void OrderCancelled() => _ordersCancelled.Add(1);

    public void Dispose() => _meter.Dispose();
}
