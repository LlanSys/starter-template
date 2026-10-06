using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException()
    {
    }

    public DomainException(string message) : base(message: message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }


}
