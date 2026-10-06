namespace ST.Application.Exceptions;

public class ServiceUnavailableException : CustomException
{
    public ServiceUnavailableException(string message = null!) : base(message: message)
    {
    }

    public ServiceUnavailableException()
    {
    }

    public ServiceUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
