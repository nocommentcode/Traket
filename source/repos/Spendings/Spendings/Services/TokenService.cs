using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Spendings.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Spendings.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;

        public TokenService(IConfiguration configuration, UserManager<User> userManager)
        {
            _configuration = configuration;
            _userManager = userManager;
        }

        public string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Authentication:Jwt:Key"]));

            var jwtToken = new JwtSecurityToken(
                issuer: _configuration["Authentication:Jwt:Issuer"],
                audience: "Anyone",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Authentication:Jwt:AccessTokenDurationInMinutes"])),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(jwtToken);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = true,
                ValidIssuer = _configuration["Authentication:Jwt:Issuer"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Authentication:Jwt:Key"])),
                ValidateLifetime = false //in this case, we don't care about the token's expiration date
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (!(securityToken is JwtSecurityToken jwtSecurityToken) || !string.Equals(jwtSecurityToken.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }

        public TokenWithClaimsPrincipal GenerateAccessTokenWithClaimsPrincipal(IList<Claim> userClaims)
        {
            var accessToken = this.GenerateAccessToken(userClaims);
            var claimsPrincipal = new ClaimsPrincipal();
            claimsPrincipal.AddIdentity(new ClaimsIdentity(userClaims));

            return new TokenWithClaimsPrincipal()
            {
                AccessToken = accessToken,
                ClaimsPrincipal = claimsPrincipal,
                AuthProperties = CreateAuthProperties(accessToken)
            };
        }

        private static AuthenticationProperties CreateAuthProperties(string accessToken)
        {
            var authProps = new AuthenticationProperties();
            authProps.StoreTokens(
                new[]
                {
                new AuthenticationToken()
                {
                    Name = "access_token",
                    Value = accessToken
                }
                });

            return authProps;
        }

        public async Task<IList<Claim>> BuildClaims(User user)
        {
            var claims = new List<Claim>
        {
            new Claim("sub", user.Id.ToString()),
            new Claim("email", user.Email),
            new Claim("user-guid", user.UserGuid.ToString()),
        };

            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim("role", userRole));
            }

            return claims;
        }
    }
}


public sealed class TokenWithClaimsPrincipal
{
    public string AccessToken { get; internal set; }

    public ClaimsPrincipal ClaimsPrincipal { get; internal set; }

    public AuthenticationProperties AuthProperties { get; internal set; }
}
