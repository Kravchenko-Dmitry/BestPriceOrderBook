using OrderBookAlgorithm.DomainClasses;

namespace OrderBookConsole;

public static class MenuHelper
{
    public static OrderType GetOrderTypeFromUser(OrderType defaultType)
    {
        while (true)
        {
            Console.WriteLine($"Select order type (default: {defaultType})");
            Console.WriteLine("1. Buy");
            Console.WriteLine("2. Sell");
            Console.Write("Your choice: ");

            var input = Console.ReadLine();

            // Default on Enter
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine($"✅ Your choice: {defaultType}\n");
                return defaultType;
            }

            switch (input.Trim())
            {
                case "1":
                    Console.WriteLine($"✅ Your choice: {OrderType.Buy}\n");
                    return OrderType.Buy;

                case "2":
                    Console.WriteLine($"✅ Your choice: {OrderType.Sell}\n");
                    return OrderType.Sell;

                default:
                    Console.WriteLine("❌ Invalid choice. Please enter 1, 2, or press Enter for default.\n");
                    break;
            }
        }
    }

    public static decimal GetOrderAmountFromUser(decimal defaultAmount)
    {
        while (true)
        {
            Console.WriteLine($"Enter order amount in BTC (default: {defaultAmount}): ");
            var input = Console.ReadLine();

            // Default on Enter
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine($"✅ Your choice: {defaultAmount} BTC\n");
                return defaultAmount;
            }

            if (decimal.TryParse(input, out var amount) && amount > 0)
            {
                Console.WriteLine($"✅ Your choice: {amount} BTC\n");
                return amount;
            }

            Console.WriteLine("❌ Invalid amount. Please enter a number greater than 0.\n");
        }
    }
}
