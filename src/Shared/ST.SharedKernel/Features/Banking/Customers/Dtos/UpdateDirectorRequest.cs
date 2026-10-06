using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ST.SharedKernel.Features.Banking.Customers.Dtos;

public record UpdateDirectorRequest(
    Guid DirectorId,
    Guid CustomerId,
    string FullName,
    string RelationType,
    string IdentificationType,
    string IdentificationNumber,
    string? PhoneNumber,
    string? Email,
    decimal? SharePercentage
);
