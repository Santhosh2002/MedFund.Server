using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MedFund.Application.Common;
using MedFund.Application.Partnerships;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace MedFund.Tests;

public sealed class PartnershipsApiTests
{
    [Fact]
    public async Task Create_partnership_lead_returns_created_for_valid_healthcare_provider()
    {
        await using var factory = new PartnershipsApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/partnerships", new
        {
            firstName = "Priya",
            lastName = "Nair",
            phoneNumber = "+91 98765 43210",
            email = "priya@citycare.example",
            partnerType = "HEALTHCARE_PROVIDER",
            organizationName = "CityCare Multispeciality Hospital"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreatePartnershipLeadResponse>();
        body.Should().NotBeNull();
        body!.Message.Should().Be("We will reach out shortly.");
    }

    [Theory]
    [InlineData("email", "not-an-email")]
    [InlineData("organizationName", "")]
    [InlineData("partnerType", "BANK")]
    public async Task Create_partnership_lead_returns_bad_request_for_invalid_payload(string field, string value)
    {
        await using var factory = new PartnershipsApiFactory();
        var client = factory.CreateClient();
        var request = new Dictionary<string, object?>
        {
            ["firstName"] = "Priya",
            ["lastName"] = "Nair",
            ["phoneNumber"] = "+91 98765 43210",
            ["email"] = "priya@citycare.example",
            ["partnerType"] = "HEALTHCARE_PROVIDER",
            ["organizationName"] = "CityCare Multispeciality Hospital"
        };
        request[field] = value;

        var response = await client.PostAsJsonAsync("/api/partnerships", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed class PartnershipsApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:ApplyMigrationsOnStartup"] = "false",
                    ["Database:SeedDevelopmentDataOnStartup"] = "false"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPartnershipService>();
                services.AddSingleton<IPartnershipService, FakePartnershipService>();
            });
        }
    }

    private sealed class FakePartnershipService : IPartnershipService
    {
        public Task<CreatePartnershipLeadResponse> CreateAsync(
            CreatePartnershipLeadRequest request,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new CreatePartnershipLeadResponse(Guid.NewGuid(), "We will reach out shortly."));
        }
    }
}
