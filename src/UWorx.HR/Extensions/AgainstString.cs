using System;

namespace UWorx.HR.Extensions;

public static partial class AgainstStringExtensions
{
    public static void NullOrEmpty(this Guards guards, string argument, string argumentName)
    {
        if (string.IsNullOrEmpty(argument))
        {
            throw new ArgumentNullException(argumentName);
        }
    }

    public static void NullOrWhiteSpace(this Guards guards, string argument, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            throw new ArgumentNullException(argumentName);
        }
    }

    public static void LeadingAndTailingSpace(this Guards guards, string argument, string argumentName)
    {
        if (argument.Trim() != argument)
        {
            throw new ArgumentException(string.Format("{0} is not allowing leading and tailing space", argumentName));
        }
    }

    public static void MinimumLength(this Guards guards, string argument, string argumentName, int minLength)
    {
        if (argument.Length < minLength)
        {
            throw new ArgumentException(string.Format("{0} is not allowing characters less than {1}", argumentName, minLength));
        }
    }

    public static void MaximumLength(this Guards guards, string argument, string argumentName, int maxLength)
    {
        if (argument.Length > maxLength)
        {
            throw new ArgumentException(string.Format("{0} is not allowing characters more than {1}", argumentName, maxLength));
        }
    }
}
