using Gossamer.Logging;

namespace Gossamer;

/// <summary>
/// Represents a command argument with a name and value.
/// <para> Arguments are parsed from strings starting with a hyphen (-) or double hyphen (--) and followed by a string value.</para>
/// </summary>
/// <param name="Name"></param>
/// <param name="Value"></param>
public record struct CommandArgument(string Name, string Value);

public interface ICommandListener
{
    void Command(string commandAction, string commandType, CommandArgument arguments);
}

class CommandManager
{
    readonly Logger logger = Core.GetLogger(nameof(CommandManager));

    readonly HashSet<ICommandListener> listeners = [];

    public void Parse(ReadOnlySpan<char> input)
    {
        input = SkipWhitespace(input);

        ReadOnlySpan<char> commandAction = ReadUntilWhitespace(input);

        if (commandAction.Length == 0)
        {
            return;
        }

        input = SkipWhitespace(input[commandAction.Length..]);

        ReadOnlySpan<char> commandType = ReadUntilWhitespace(input);

        if (commandType.Length == 0)
        {
            return;
        }

        input = SkipWhitespace(input[commandType.Length..]);

        // Note that arguments are strings starting with a hyphen (-) or double hyphen (--) and followed by a string value.
        List<CommandArgument> arguments = [];

        while (input.Length > 0)
        {
            ReadOnlySpan<char> argumentName = ReadUntilWhitespace(input);

            logger.Debug($"Argument name: {argumentName.ToString()}");

            if (IsArgumentName(argumentName, out bool isLong))
            {
                input = SkipWhitespace(input[argumentName.Length..]);

                ReadOnlySpan<char> argumentValue = ReadUntilWhitespace(input);

                logger.Debug($"Argument value: {argumentValue.ToString()}");

                // Not all arguments have values, so we need to check if the next token is a valid argument name.

                if (!IsArgumentName(argumentValue, out _))
                {
                    arguments.Add(new CommandArgument(argumentName.ToString(), argumentValue.ToString()));
                }

                input = SkipWhitespace(input[argumentValue.Length..]);
            }
            else if (argumentName.Length == 0)
            {
                break;
            }
            else
            {
                // Invalid argument name, log a warning and skip to the next token.
                logger.Warning($"Invalid argument name '{argumentName.ToString()}'.");
                input = SkipWhitespace(input[argumentName.Length..]);
            }
        }

        // Log the command
        logger.Debug($"Command: {commandAction.ToString()} {commandType.ToString()} {string.Join(", ", arguments.Select(a => $"{a.Name}={a.Value}"))}");
    }

    static ReadOnlySpan<char> ReadUntilWhitespace(ReadOnlySpan<char> input)
    {
        int endIndex = 0;
        while (endIndex < input.Length && !char.IsWhiteSpace(input[endIndex]))
        {
            endIndex++;
        }
        return input[0..endIndex];
    }

    static ReadOnlySpan<char> SkipWhitespace(ReadOnlySpan<char> input)
    {
        int startIndex = 0;
        while (startIndex < input.Length && char.IsWhiteSpace(input[startIndex]))
        {
            startIndex++;
        }
        return input[startIndex..];
    }

    static bool IsArgumentName(ReadOnlySpan<char> input, out bool isLong)
    {
        isLong = false;

        if (input.Length == 0)
        {
            return false;
        }

        if (input[0] == '-')
        {
            if (input.Length > 1 && input[1] == '-')
            {
                isLong = true;
                return true;
            }
            return true;
        }

        return false;
    }
}