using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Exceptions;

public class EmailServiceException : CustomException
{
    public EmailServiceException(string message = null!) : base(message: message)
    {
    }

    public EmailServiceException()
    {
    }

    public EmailServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
