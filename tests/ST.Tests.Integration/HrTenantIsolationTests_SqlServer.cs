using ST.Domain.Features.HR.Departments.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.HR.DataContext;
using ST.Tests.Integration.TestFixtures;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ST.Tests.Integration;

public class HrTenantIsolationTests_SqlServer : HrTenantIsolationTests<MsSqlDbFixture>
{
    public HrTenantIsolationTests_SqlServer(MsSqlDbFixture fixture) : base(fixture) { }
}
