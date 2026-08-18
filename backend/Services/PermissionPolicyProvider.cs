using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace SmartBizERP.Api.Services;

public class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public const string Prefix = "permission:";

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return base.GetPolicyAsync(policyName);

        var permission = policyName[Prefix.Length..];

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim("permission", permission)
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
