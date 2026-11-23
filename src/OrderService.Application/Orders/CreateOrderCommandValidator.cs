namespace OrderService.Application.Orders;

public static class CreateOrderCommandValidator
{
    public static Dictionary<string, string[]> Validate(CreateOrderCommand command)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(command.CustomerName))
            errors["customerName"] = ["Customer name is required."];

        if (command.Amount is null)
            errors["amount"] = ["Amount is required."];

        return errors;
    }
}
