using ST.SharedKernel.Extensions;
using ST.Domain.Shared.Entities;
using ST.SharedKernel.Features.Shared.EmailTemplates.Dtos;
using ST.SharedKernel.Features.Shared.EmailTemplates.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.Shared.EmailTemplates.Mappings;

public static class EmailTemplateMapping
{
    public static EmailTemplateResponse ToEmailTemplateResponse(this EmailTemplate entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new EmailTemplateResponse
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            Name = entity.Name,
            Subject = entity.Subject,
            Body = entity.Body
        };
    }

    public static EmailTemplateType ToEnum(this EmailTemplate entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return entity.Name.ToEnum<EmailTemplateType>();
    }

    public static EmailTemplateType ToTemplateEnum(this string templateName)
    {
        ArgumentNullException.ThrowIfNull(templateName);

        return templateName.ToEnum<EmailTemplateType>();
    }

    public static string ToTemplateName(this EmailTemplateType enumValue)
    {
        return enumValue.ToDisplayString();
    }
}
