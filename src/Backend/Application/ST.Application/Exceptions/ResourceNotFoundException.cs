namespace ST.Application.Exceptions;
public class ResourceNotFoundException : CustomException
{
    public ResourceNotFoundException(string message = null!) : base(message: message)
    {
    }

    public ResourceNotFoundException()
    {
    }

    public ResourceNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
