using ST.Domain.Features.Shared.OrgSettings.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.Shared.DataContext;
using ST.Tests.Integration.TestFixtures;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ST.Tests.Integration;

public class SharedTenantIsolationTests_SqlServer : SharedTenantIsolationTests<MsSqlDbFixture>
{
    public SharedTenantIsolationTests_SqlServer(MsSqlDbFixture fixture) : base(fixture) { }
}
