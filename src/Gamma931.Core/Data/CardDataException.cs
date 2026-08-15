namespace Gamma931.Core.Data;

/// <summary>Thrown when a card CSV is missing, malformed, or fails validation on load.</summary>
public sealed class CardDataException : Exception
{
    public CardDataException(string message) : base(message)
    {
    }

    public CardDataException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
