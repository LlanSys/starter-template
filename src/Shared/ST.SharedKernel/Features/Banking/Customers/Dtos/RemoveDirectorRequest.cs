using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Features.Banking.Customers.Dtos;

public record RemoveDirectorRequest(Guid CustomerId, Guid DirectorId);
