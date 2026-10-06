using ST.Domain.Features.Banking.Customers.Entities;
using ST.Domain.Features.Banking.Customers.Enums;
using ST.Domain.Features.Banking.Customers.ValueObjects;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.Banking.DataContext;
using ST.Tests.Integration.TestFixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ST.Tests.Integration;

public class BankingTenantIsolationTests_SqlServer : BankingTenantIsolationTests<MsSqlDbFixture>
{
    public BankingTenantIsolationTests_SqlServer(MsSqlDbFixture fixture) : base(fixture) { }
}
