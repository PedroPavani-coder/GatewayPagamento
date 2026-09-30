using System.Net.Http.Json;

namespace PaymentGateway.IntegrationTests;

/// <summary>
/// Como o OrdersController agora exige [Authorize], todo teste que for chamar um
/// endpoint de pedido precisa primeiro "fazer login" e anexar o token no header
/// Authorization — exatamente como faria um cliente real.
/// </summary>
public static class AuthHelper
{
    public static async Task<string> ObterTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = "admin",
            password = "admin123"
        });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        return body!.Token;
    }

    private record LoginResponseDto(string Token, DateTime ExpiresAt);
}
