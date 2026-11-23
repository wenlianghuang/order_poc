using OrderService.Domain.Exceptions;

namespace OrderService.Domain.Entities;

public sealed class Order
{
    public const int CustomerNameMaxLength = 200;

    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string CustomerName { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Order()
    {
    }

    public static Order Create(Guid ownerId, string customerName, decimal amount)
    {
        if (ownerId == Guid.Empty)
            throw new DomainException("Owner is required.");

        var (normalizedName, normalizedAmount) = NormalizeDetails(customerName, amount);

        return new Order
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            CustomerName = normalizedName,
            Amount = normalizedAmount,
            Status = OrderStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void ChangeDetails(string customerName, decimal amount)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("Only a draft order can be changed.");

        var (normalizedName, normalizedAmount) = NormalizeDetails(customerName, amount);
        CustomerName = normalizedName;
        Amount = normalizedAmount;
    }

    private static (string Name, decimal Amount) NormalizeDetails(string customerName, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("Customer name is required.");

        var normalizedName = customerName.Trim();
        if (normalizedName.Length > CustomerNameMaxLength)
            throw new DomainException($"Customer name must be {CustomerNameMaxLength} characters or fewer.");

        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero.");

        return (normalizedName, amount);
    }

    public void Pay()
    {
        if (Status == OrderStatus.Paid)
            throw new DomainException("Order is already paid.");

        if (Status != OrderStatus.Draft)
            throw new DomainException("Only a draft order can be paid.");

        Status = OrderStatus.Paid;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            throw new DomainException("Order is already cancelled.");

        if (Status != OrderStatus.Draft)
            throw new DomainException("Only a draft order can be cancelled.");

        Status = OrderStatus.Cancelled;
    }
}
