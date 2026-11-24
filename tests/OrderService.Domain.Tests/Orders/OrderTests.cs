using OrderService.Domain.Entities;
using OrderService.Domain.Exceptions;

namespace OrderService.Domain.Tests.Orders;

public sealed class OrderTests
{
    private static readonly Guid OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void Create_starts_a_draft_for_that_owner()
    {
        var before = DateTime.UtcNow;

        var order = Order.Create(OwnerId, "  Ada  ", 120);

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(OwnerId, order.OwnerId);
        Assert.Equal("Ada", order.CustomerName);
        Assert.Equal(120, order.Amount);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.InRange(order.CreatedAtUtc, before, DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_customer_name(string? customerName)
    {
        var exception = Assert.Throws<DomainException>(() => Order.Create(OwnerId, customerName!, 120));

        Assert.Equal("Customer name is required.", exception.Message);
    }

    [Fact]
    public void Create_rejects_a_customer_name_longer_than_200_characters()
    {
        var customerName = new string('a', Order.CustomerNameMaxLength + 1);

        var exception = Assert.Throws<DomainException>(() => Order.Create(OwnerId, customerName, 120));

        Assert.Equal($"Customer name must be {Order.CustomerNameMaxLength} characters or fewer.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_rejects_an_amount_that_is_not_positive(int amount)
    {
        var exception = Assert.Throws<DomainException>(() => Order.Create(OwnerId, "Ada", amount));

        Assert.Equal("Amount must be greater than zero.", exception.Message);
    }

    [Fact]
    public void Create_rejects_an_empty_owner()
    {
        var exception = Assert.Throws<DomainException>(() => Order.Create(Guid.Empty, "Ada", 120));

        Assert.Equal("Owner is required.", exception.Message);
    }

    [Fact]
    public void ChangeDetails_updates_a_draft()
    {
        var order = Draft();

        order.ChangeDetails("  Grace  ", 80);

        Assert.Equal("Grace", order.CustomerName);
        Assert.Equal(80, order.Amount);
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public void ChangeDetails_rejects_a_paid_order_and_leaves_it_unchanged()
    {
        var order = Draft();
        order.Pay();

        AssertRejected(
            () => order.ChangeDetails("Grace", 80),
            "Only a draft order can be changed.",
            order,
            "Ada",
            120,
            OrderStatus.Paid);
    }

    [Fact]
    public void ChangeDetails_rejects_a_cancelled_order_and_leaves_it_unchanged()
    {
        var order = Draft();
        order.Cancel();

        AssertRejected(
            () => order.ChangeDetails("Grace", 80),
            "Only a draft order can be changed.",
            order,
            "Ada",
            120,
            OrderStatus.Cancelled);
    }

    [Fact]
    public void Pay_moves_a_draft_to_paid()
    {
        var order = Draft();

        order.Pay();

        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public void Pay_rejects_an_already_paid_order_and_leaves_it_paid()
    {
        var order = Draft();
        order.Pay();

        AssertRejected(
            order.Pay,
            "Order is already paid.",
            order,
            "Ada",
            120,
            OrderStatus.Paid);
    }

    [Fact]
    public void Pay_rejects_a_cancelled_order_and_leaves_it_cancelled()
    {
        var order = Draft();
        order.Cancel();

        AssertRejected(
            order.Pay,
            "Only a draft order can be paid.",
            order,
            "Ada",
            120,
            OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_moves_a_draft_to_cancelled()
    {
        var order = Draft();

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_rejects_an_already_cancelled_order_and_leaves_it_cancelled()
    {
        var order = Draft();
        order.Cancel();

        AssertRejected(
            order.Cancel,
            "Order is already cancelled.",
            order,
            "Ada",
            120,
            OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_rejects_a_paid_order_and_leaves_it_paid()
    {
        var order = Draft();
        order.Pay();

        AssertRejected(
            order.Cancel,
            "Only a draft order can be cancelled.",
            order,
            "Ada",
            120,
            OrderStatus.Paid);
    }

    private static Order Draft() => Order.Create(OwnerId, "Ada", 120);

    private static void AssertRejected(
        Action action,
        string message,
        Order order,
        string customerName,
        decimal amount,
        OrderStatus status)
    {
        var exception = Assert.Throws<DomainException>(action);

        Assert.Equal(message, exception.Message);
        Assert.Equal(customerName, order.CustomerName);
        Assert.Equal(amount, order.Amount);
        Assert.Equal(status, order.Status);
    }
}
