namespace SDI.Back.Template.Exceptions;

public sealed class DomainException(string message, IReadOnlyCollection<string> errors, int statusCode = StatusCodes.Status400BadRequest) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public IReadOnlyCollection<string> Errors { get; } = errors;

    public DomainException(string message, int statusCode = StatusCodes.Status400BadRequest)
        : this(message, [message], statusCode)
    {
    }
}
