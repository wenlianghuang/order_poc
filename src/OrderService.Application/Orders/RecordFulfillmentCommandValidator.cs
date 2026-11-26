using OrderService.Domain.Entities;

namespace OrderService.Application.Orders;

public static class RecordFulfillmentCommandValidator
{
    public static Dictionary<string, string[]> Validate(RecordFulfillmentCommand command)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(command.Fulfillment))
            errors["fulfillment"] = ["Fulfillment is required."];
        else if (!TryParse(command.Fulfillment, out _))
            errors["fulfillment"] = ["Fulfillment must be Pending, Packed, or Shipped."];

        return errors;
    }

    public static FulfillmentStatus Parse(string fulfillment)
    {
        if (!TryParse(fulfillment, out var status))
            throw new InvalidOperationException($"Fulfillment is not a known status: {fulfillment}");

        return status;
    }

    private static bool TryParse(string value, out FulfillmentStatus fulfillment)
    {
        switch (value)
        {
            case nameof(FulfillmentStatus.Pending):
                fulfillment = FulfillmentStatus.Pending;
                return true;
            case nameof(FulfillmentStatus.Packed):
                fulfillment = FulfillmentStatus.Packed;
                return true;
            case nameof(FulfillmentStatus.Shipped):
                fulfillment = FulfillmentStatus.Shipped;
                return true;
            default:
                fulfillment = default;
                return false;
        }
    }
}
