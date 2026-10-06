using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Common.Interceptors;
using ST.Persistence.Features.Shared.DataContext;
using ST.Tests.Integration.TestFixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ST.Tests.Integration;

public class TenantConnectionInterceptorIntegrationTests_SqlServer : TenantConnectionInterceptorIntegrationTests<MsSqlDbFixture>
{
    public TenantConnectionInterceptorIntegrationTests_SqlServer(MsSqlDbFixture fixture) : base(fixture) { }
}
