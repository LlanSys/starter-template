namespace ST.Application.Exceptions;
public class CreatingDuplicateException : CustomException
{
    public CreatingDuplicateException(string message = null!) : base(message: message)
    {
    }

    public CreatingDuplicateException()
    {
    }

    public CreatingDuplicateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}