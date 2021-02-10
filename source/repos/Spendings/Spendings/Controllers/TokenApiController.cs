using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Spendings.Data;
using Spendings.Models;
using Spendings.Services;

namespace Spendings.Controllers
{
    [ApiController]
    public class TokenApiController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly IRepository<UserRefreshToken> _refreshTokenRepository;

        public TokenApiController (
            UserManager<User> userManager,
            ITokenService tokenService,
            IConfiguration configuration,
            IRepository<UserRefreshToken> refreshTokenRepository)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _configuration = configuration;
            _tokenService = tokenService;
            _userManager = userManager;
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost("/api/token/create")]
        public async Task<IActionResult> CreateToken([FromBody] TokenLoginModel login, bool includeRefreshToken)
        {
            var user = await _userManager.FindByNameAsync(login.Username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, login.Password))
            {
                return BadRequest("Invalid username or password");
            }

            var claims = await BuildClaims(user);

            var token = _tokenService.GenerateAccessToken(claims);
            if (includeRefreshToken)    
            {
                var refreshToken = _tokenService.GenerateRefreshToken();
                var userRefreshToken = new UserRefreshToken()
                {
                    User = user,
                    RefreshTokenHash = _userManager.PasswordHasher.HashPassword(user, refreshToken),
                    DateCreated = DateTime.UtcNow,
                    DateExpires = DateTime.UtcNow.AddDays(7)
                };
                await _refreshTokenRepository.Add(userRefreshToken);
                await _userManager.UpdateAsync(user);
                return Ok(new { token, refreshToken, firstName = user.Name, lastName = user.Surname, validTill = DateTimeOffset.UtcNow.AddMinutes(int.Parse(_configuration["Authentication:Jwt:AccessTokenDurationInMinutes"])).UtcDateTime });
            }

            return Ok(new { token });
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost("/api/token/refresh")]
        public async Task<IActionResult> RefeshToken(RefreshTokenModel model)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(model.Token);
            if (principal == null)
            {
                return BadRequest(new { Error = "Invalid token" });
            }

            var user = await _userManager.GetUserAsync(principal);
            var userRefreshTokens = await _refreshTokenRepository.Query()
                .Include(x => x.User)
                .Where(x => x.User.Id == user.Id && x.DateExpires > DateTime.UtcNow)
                .Select(x => x.RefreshTokenHash)
                .ToListAsync();

            foreach(var tokenHash in userRefreshTokens)
            {
                var verifyRefreshTokenResult = _userManager.PasswordHasher.VerifyHashedPassword(user, tokenHash, model.RefreshToken);
                if (verifyRefreshTokenResult == PasswordVerificationResult.Success)
                {
                    var claims = await BuildClaims(user);
                    var newToken = _tokenService.GenerateAccessToken(claims);
                    return Ok(new { token = newToken, firstName = user.Name, lastName = user.Surname, validTill = DateTimeOffset.UtcNow.AddMinutes(int.Parse(_configuration["Authentication:Jwt:AccessTokenDurationInMinutes"])) });
                }
            }

            return Forbid();
        }
        private async Task<IList<Claim>> BuildClaims(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("UserGuid", user.UserGuid.ToString()),
            };

            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            return claims;
        }

        public class TokenLoginModel
        {
            [Required(ErrorMessage = "The {0} field is required.")]
            public string Username { get; set; }

            [Required(ErrorMessage = "The {0} field is required.")]
            public string Password { get; set; }
        }

        public class RefreshTokenModel
        {
            [Required(ErrorMessage = "The {0} field is required.")]
            public string Token { get; set; }

            [Required(ErrorMessage = "The {0} field is required.")]
            public string RefreshToken { get; set; }
        }
    }
}
