using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Features.Shared.EmailTemplates.Dtos;

public record EmailTemplateResponse
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
}
