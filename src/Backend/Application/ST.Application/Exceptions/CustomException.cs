using System.Collections.ObjectModel;
using System.Net;

namespace ST.Application.Exceptions;
public class CustomException : Exception
{
    public Collection<string>? ErrorMessages { get; }
    public HttpStatusCode StatusCode { get; }

    public CustomException()
    {

    }

    public CustomException(
        string message,
        Collection<string>? errorMessages = default,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError) : base(message)
    {
        ErrorMessages = errorMessages;
        StatusCode = statusCode;
    }

    public CustomException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public CustomException(string message) : base(message)
    {
    }
}
