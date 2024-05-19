using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices.ClientServices;

public class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly RestClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<ApiAuthenticationStateProvider> _logger;

    public ApiAuthenticationStateProvider(RestClient httpClient, ILocalStorageService localStorage,
        ILogger<ApiAuthenticationStateProvider> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var savedToken = await _localStorage.GetItemAsync<string>("authToken");

        if (string.IsNullOrWhiteSpace(savedToken))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        _httpClient.AuthHeader = new AuthenticationHeaderValue("bearer", savedToken);

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(savedToken), "jwt")));
    }

    public void MarkUserAsAuthenticated()
    {
        var authState = GetAuthenticationStateAsync();

        NotifyAuthenticationStateChanged(authState);
    }

    public void MarkUserAsLoggedOut()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(anonymousUser));
        NotifyAuthenticationStateChanged(authState);
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs != null)
        {
            keyValuePairs.TryGetValue(ClaimTypes.Role, out var roles);

            if (roles != null)
            {
                if (roles.ToString().Trim().StartsWith("["))
                {
                    var parsedRoles = JsonSerializer.Deserialize<string[]>(roles.ToString());

                    if (parsedRoles != null)
                    {
                        _logger.LogInformation("Parsed roles from JWT:");
                        foreach (var parsedRole in parsedRoles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, parsedRole));
                            _logger.LogTrace($"  [{roles}]");
                        }
                    }
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, roles.ToString()));
                    _logger.LogTrace($"Parsed role from JWT: [{roles}]");
                }

                keyValuePairs.Remove(ClaimTypes.Role);
            }

            claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString())));
        }

        _logger.LogTrace(
            $"Parsed Claims from JWT: {string.Join(", ", keyValuePairs.Select(x => $"{x.Key} = {x.Value}"))}");

        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }

        return Convert.FromBase64String(base64);
    }

    public void RefreshAuthState()
    {
        var state = GetAuthenticationStateAsync();

        NotifyAuthenticationStateChanged(state);
    }
}
