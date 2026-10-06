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

public class TenantConnectionInterceptorIntegrationTests_PostgreSql : TenantConnectionInterceptorIntegrationTests<PostgreSqlDbFixture>
{
    public TenantConnectionInterceptorIntegrationTests_PostgreSql(PostgreSqlDbFixture fixture) : base(fixture) { }
}
