using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ReembolsoBAS.Auth;

public sealed class AllowAllHandler :
    AuthenticationHandler<AuthenticationSchemeOptions>
{
    public AllowAllHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> opt,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(opt, logger, encoder, clock) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 👉 1) MATRÍCULA falsa p/ testes
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "T1234"),

            // 👉 2) TODAS as roles usadas nos controllers
            new Claim(ClaimTypes.Role, "empregado"),
            new Claim(ClaimTypes.Role, "rh"),
            new Claim(ClaimTypes.Role, "gerente_rh"),
            new Claim(ClaimTypes.Role, "admin")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
