using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Contracts.Interfaces.Common;

public interface IBackgroundJobService
{
    void Enqueue(IRequest request);
    Task EnqueueAsync(IRequest? request, CancellationToken ct = default);
}
