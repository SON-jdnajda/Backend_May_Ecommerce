namespace Shop.Domain.ValueObjects;

public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Số tiền không được âm.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Mã tiền tệ phải đúng 3 ký tự ISO 4217", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency) => new(0, currency);

    public Money Add(Money other)
    {
        if (other.Currency != Currency)
            throw new InvalidOperationException($"Không thể cộng {Currency} với {other.Currency}");
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Số lượng không được âm.");

        return new Money(Amount * quantity, Currency);
    }

    public override string ToString() => $"{Amount:N0} {Currency}";

    public static Money Vnd(decimal amount) => new(amount, "VND");

    public Money Subtract(Money other)
    {
        if (other.Currency != Currency)
            throw new InvalidOperationException($"Không thể trừ {Currency} cho {other.Currency}");
        if (other.Amount > Amount)
            throw new InvalidOperationException("Kết quả phép trừ không được âm");
        return new Money(Amount - other.Amount, Currency);
    }
}
