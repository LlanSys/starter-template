using ST.Domain.Features.Shared.EmailTemplates.Entities;
using ST.Domain.Shared.Contracts.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Features.Shared.EmailTemplates.Contracts.Repositories;

public interface IEmailTemplateRepository : IRepository<EmailTemplate>
{
}
