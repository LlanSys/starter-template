using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Features.Banking.Customers.Dtos;

public record CustomerListRequest(

    Guid? Cursor = null,
    int PageSize = 50
);
