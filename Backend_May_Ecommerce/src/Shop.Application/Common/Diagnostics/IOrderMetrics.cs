namespace Shop.Application.Common.Diagnostics;

/// <summary>
/// Outbound port for order telemetry. Handlers depend on this, never on
/// System.Diagnostics.Metrics - the Application layer states WHAT is worth
/// measuring, Infrastructure decides HOW it is recorded and exported.
/// </summary>
public interface IOrderMetrics
{
    void OrderPlaced(int itemCount, decimal totalAmount);

    void OrderCancelled();
}
