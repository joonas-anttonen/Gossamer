namespace Gossamer;

public class GossamerException : Exception
{
    public GossamerException()
    {
    }

    public GossamerException(string? message) : base(message)
    {
    }

    public GossamerException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}