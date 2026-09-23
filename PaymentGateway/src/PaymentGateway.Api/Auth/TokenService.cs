using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace PaymentGateway.Api.Auth;

public interface ITokenService
{
    string GenerateToken(string userName);
}

/// <summary>
/// Gera o "crachá digital" (token JWT) depois que o login é validado.
///
/// Um token JWT tem 3 partes, separadas por pontos: Header.Payload.Signature.
/// - Header: diz qual algoritmo de assinatura foi usado
/// - Payload: os "Claims" — informações sobre quem é o usuário (aqui, só o nome)
/// - Signature: uma assinatura digital, calculada com a JwtSettings.Key, que prova
///   que o token não foi alterado por ninguém no meio do caminho
/// </summary>
public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public string GenerateToken(string userName)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpiresInMinutes),
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
