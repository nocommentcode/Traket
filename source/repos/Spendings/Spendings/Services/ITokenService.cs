using Spendings.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Spendings.Services
{
    public interface ITokenService
    {
        Task<IList<Claim>> BuildClaims(User user);
        string GenerateAccessToken(IEnumerable<Claim> claims);
        TokenWithClaimsPrincipal GenerateAccessTokenWithClaimsPrincipal(IList<Claim> userClaims);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}