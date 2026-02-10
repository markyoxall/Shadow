using Duende.IdentityServer.Models;

namespace Shadow.Identity;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new ApiScope("remote_api"),
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            // interactive client using code flow + pkce
            new Client
            {
                ClientId = "interactive",
                ClientSecrets = { new Secret("49C1A7E1-0C79-4A89-A3D6-A37998FB86B0".Sha256()) },
                
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,

                // redirect URIs: only the actual BFF host (7035) is required for local development
                RedirectUris = { "https://localhost:7035/signin-oidc" },
                FrontChannelLogoutUri = "https://localhost:7035/signout-oidc",
                PostLogoutRedirectUris = { "https://localhost:7035/signout-callback-oidc" },


                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "profile", "remote_api" }
            },
        };
}
