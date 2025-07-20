# IMPORTANT

Not endorsing, but if we want to try

## Examples

```cs
var a = Maybe<int>.Nothing();
var b = Maybe<int>.Nothing();
bool areNonesEqual = a.Equals(b); // true

var x = Maybe<int>.Something(5);
var y = Maybe<int>.Something(5);
bool areSomesEqual = x.Equals(y); // true

// A function that tries to parse an int, returns Maybe<int>
Maybe<int> TryParseInt(string input)
{
    return int.TryParse(input, out var value)
        ? Maybe<int>.Something(value)
        : Maybe<int>.Nothing();
}

// A function that divides two numbers, returns Result<int>
Result<int> SafeDivide(int numerator, int denominator)
{
    try
    {
        if (denominator == 0)
            throw new DivideByZeroException();
        return numerator / denominator;
    }
    catch (Exception ex)
    {
        return ex;
    }
}

// Usage
string input = "42";
var maybeInt = TryParseInt(input);

var result = maybeInt.Bind(num =>
    SafeDivide(num, 2) // Only divides if parsing succeeded
);

if (result is Result<int> r)
{
    int final = r.Match(
        onSuccess: v => v,
        onFailure: ex => -1,
        onNull: () => 0
    );
    // final will be 21 if all succeeded, -1 if division failed, 0 if input was not a number
}
```

Mixing the two together

```cs
public class AccountService
{
    // Simulated data source
    private readonly Dictionary<string, int?> _accounts = new()
    {
        { "A123", 1000 },
        { "B456", null }, // Account exists, but no balance
        // "C789" does not exist
    };

    public Result<Maybe<int>> GetBalance(string account)
    {
        try
        {
            // Simulate a system error
            if (account == "ERROR")
                throw new Exception("Database connection failed.");

            if (_accounts.TryGetValue(account, out var balance))
            {
                if (balance.HasValue)
                    return Maybe<int>.Something(balance.Value); // Success, has balance
                else
                    return Maybe<int>.Nothing(); // Success, but no balance
            }
            else
            {
                return Maybe<int>.Nothing(); // Success, but account not found
            }
        }
        catch (Exception ex)
        {
            return ex; // Failure
        }
    }
}

// Usage
var service = new AccountService();

var result = service.GetBalance("A123");
result.Match(
    onSuccess: maybe => maybe.Match(
        onSome: bal => $"Balance: {bal}",
        onNone: () => "No balance or account not found."
    ),
    onFailure: ex => $"Error: {ex.Message}",
    onNull: () => "Unknown result."
);

// Try with an error
var errorResult = service.GetBalance("ERROR");
errorResult.Match(
    onSuccess: maybe => "Should not happen.",
    onFailure: ex => $"Error: {ex.Message}",
    onNull: () => "Unknown result."
);
```