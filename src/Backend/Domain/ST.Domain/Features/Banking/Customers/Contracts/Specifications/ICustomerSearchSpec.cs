using ST.Domain.Shared.Contracts.Specifications;
using ST.Domain.Features.Banking.Customers.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Features.Banking.Customers.Contracts.Specifications;

public interface ICustomerSearchSpec : ISpecification<Customer, Guid>
{ }
