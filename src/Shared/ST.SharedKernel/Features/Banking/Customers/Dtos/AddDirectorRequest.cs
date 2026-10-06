using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Features.Banking.Customers.Dtos;

public record AddDirectorRequest(
    Guid CustomerId,
    string FullName,
    string RelationType,
    string IdentificationType,
    string IdentificationNumber,
    string? PhoneNumber,
    string? Email,
    decimal? SharePercentage
);
