using ST.Domain.Features.IAM.Menus.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.IAM.DataContext;
using ST.Tests.Integration.TestFixtures;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ST.Tests.Integration;

public class TenantIsolationMutationTests_SqlServer : TenantIsolationMutationTests<MsSqlDbFixture>
{
    public TenantIsolationMutationTests_SqlServer(MsSqlDbFixture fixture) : base(fixture) { }
}
