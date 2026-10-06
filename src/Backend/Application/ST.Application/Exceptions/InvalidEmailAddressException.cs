using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Exceptions;

public class InvalidEmailAddressException : CustomException
{
    public InvalidEmailAddressException(string message = null!) : base(message: message)
    {
    }

    public InvalidEmailAddressException()
    {
    }

    public InvalidEmailAddressException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
