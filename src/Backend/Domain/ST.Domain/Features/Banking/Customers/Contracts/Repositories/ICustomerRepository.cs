using ST.Domain.Shared.Contracts.Repositories;
using ST.Domain.Features.Banking.Customers.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Features.Banking.Customers.Contracts.Repositories;

public interface ICustomerRepository : IRepository<Customer>
{

}
