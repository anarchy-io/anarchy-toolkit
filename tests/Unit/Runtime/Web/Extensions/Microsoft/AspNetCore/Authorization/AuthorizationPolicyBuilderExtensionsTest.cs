using Anarchy.Toolkit.Runtime.Web.Extensions.Microsoft.AspNetCore.Authorization;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Anarchy.Toolkit.Tests.Unit.Runtime.Web.Extensions.Microsoft.AspNetCore.Authorization;

public class AuthorizationPolicyBuilderExtensionsTest
{
    [Fact]
    public async Task RequireScope_MultipleScopeClaims_Succeeds()
    {
        var succeeded = await AuthorizeAsync(new Claim("scope", "other"), new Claim("scope", "api.read"));

        succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task RequireScope_SpaceSeparatedScopeClaim_Succeeds()
    {
        var succeeded = await AuthorizeAsync(new Claim("scope", "other api.write"));

        succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task RequireScope_ScpClaim_Succeeds()
    {
        var succeeded = await AuthorizeAsync(new Claim("scp", "api.read"));

        succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task RequireScope_NoMatchingScope_Fails()
    {
        var succeeded = await AuthorizeAsync(new Claim("scope", "other api.admin"), new Claim("role", "api.read"));

        succeeded.Should().BeFalse();
    }

    private static async Task<bool> AuthorizeAsync(params Claim[] claims)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddAuthorization(options => options.AddPolicy("p", p => p.RequireScope("api.read", "api.write")))
            .BuildServiceProvider();
        var authorization = services.GetRequiredService<IAuthorizationService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var result = await authorization.AuthorizeAsync(user, "p");

        return result.Succeeded;
    }
}
