namespace PaymentGateway.Api.Auth;

/// <summary>
/// Representa as configurações necessárias pra gerar e validar tokens JWT.
/// Os valores de verdade vêm do appsettings.json (seção "Jwt").
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// A "chave secreta" usada pra assinar o token digitalmente. Quem não tiver essa
    /// mesma chave não consegue gerar um token válido nem forjar um. Em produção,
    /// isso NUNCA deveria estar num arquivo versionado — viria de variável de ambiente
    /// ou de um cofre de segredos (Azure Key Vault, AWS Secrets Manager, etc).
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Quem emitiu o token (nossa própria Api).</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Para quem esse token é válido (quem pode usá-lo).</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Depois de quantos minutos o token expira.</summary>
    public int ExpiresInMinutes { get; set; } = 60;
}
