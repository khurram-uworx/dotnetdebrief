using System;

namespace UWorx.HR;

public class Guards
{
    public static Guards Against { get; } = new Guards();

    public void Null<T>(T argument, string argumentName)
    {
        if (argument == null)
        {
            throw new ArgumentNullException(argumentName);
        }
    }

    public void Null<T>(T argument) =>
        Null(argument, nameof(argument));
}
