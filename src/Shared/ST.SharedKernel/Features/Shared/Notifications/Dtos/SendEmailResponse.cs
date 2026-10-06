using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Features.Shared.Notifications.Dtos;

public record SendEmailResponse(
    string MessageId,
    DateTimeOffset SentAt,
    string Recipient,
    string Subject
);
