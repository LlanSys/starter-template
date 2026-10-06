using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Features.Shared.Lookups.Dtos;

public record LookupResponse(int Id, string Code, string? Description, int DisplayOrder = 0);
