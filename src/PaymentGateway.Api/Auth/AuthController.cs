using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace PaymentGateway.Api.Auth;

public record LoginRequest(string UserName, string Password);
public record LoginResponse(string Token, DateTime ExpiresAt);

/// <summary>
/// Endpoint de login. Repare que esse Controller NÃO tem [Authorize] — faz sentido,
/// já que é justamente aqui que alguém que ainda não está autenticado precisa entrar.
///
/// IMPORTANTE — simplificação proposital: esse projeto não tem uma tabela de usuários
/// no banco. O login abaixo usa uma credencial fixa, só pra demonstrar o mecanismo de
/// autenticação JWT funcionando de ponta a ponta. Numa aplicação real, aqui entraria
/// uma consulta a uma tabela de Usuários, com senha armazenada com hash (nunca em
/// texto puro) — por exemplo, usando ASP.NET Core Identity.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    public AuthController(ITokenService tokenService, IOptions<JwtSettings> jwtOptions)
    {
        _tokenService = tokenService;
        _jwtSettings = jwtOptions.Value;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Credencial de demonstração: usuário "admin", senha "admin123".
        var credencialValida = request.UserName == "admin" && request.Password == "admin123";

        if (!credencialValida)
            return Unauthorized(new { error = "Usuário ou senha inválidos." });

        var token = _tokenService.GenerateToken(request.UserName);
        var expiraEm = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes);

        return Ok(new LoginResponse(token, expiraEm));
    }
}
