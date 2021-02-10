using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Spendings.Models;
using Spendings.Services;

namespace Spendings.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class UserController : Controller
    {
        private UserManager<User> _userManager;
        private RoleManager<Role> _roleManager;
        private readonly IConfiguration _config;
        private readonly ITokenService _tokenService;
        private readonly IUserService _userService;


        public UserController (
            UserManager<User> userManager,
            IConfiguration config,
            ITokenService tokenService,
            RoleManager<Role> roleManager,
            IUserService userService)
        {
            _roleManager = roleManager;
            _tokenService = tokenService;
            _userManager = userManager;
            _config = config;
            _userService = userService;
        }

        [HttpGet("/api/user")]
        public async Task<IActionResult> Get()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (user == null)
            {
                return NotFound();
            }

            var model = new UserForm
            {
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                RoleIds = user.Roles.Select(x => x.RoleId).ToList(),
            };

            return Json(model);
        }

        [HttpPost("/api/user/register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterAjaxViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.Values.SelectMany(z => z.Errors.Select(x => x.ErrorMessage)).FirstOrDefault());
            }

            var user = new User()
            {
                UserName = model.Email,
                Email = model.Email,
                UserGuid = model.UserGuid,
                Name = model.Name,
                Surname = model.Surname,  
                TimeZoneId = model.TimeZoneId,
                IsDeleted = false,
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // addto user role
                await _userManager.AddToRoleAsync(user, "User");


                // add default category
                await _userService.SetInitialCategory(user);

                var claims = await _tokenService.BuildClaims(user);
                var jwtToken = _tokenService.GenerateAccessToken(claims);
                var refreshToken = _tokenService.GenerateRefreshToken();
                user.RefreshTokenHash = _userManager.PasswordHasher.HashPassword(user, refreshToken);
                await _userManager.UpdateAsync(user);
                return Ok(new { token = jwtToken, refreshToken , firstName = user.Name, lastName = user.Surname, validTill = DateTimeOffset.UtcNow.AddMinutes(int.Parse(_config["Authentication:Jwt:AccessTokenDurationInMinutes"])) });
            }

            return BadRequest(result.Errors.Select(z => z.Description.Replace("User name", "Email")).FirstOrDefault());
        }

         
        [HttpDelete("/api/user/{id}")]
        public async Task<IActionResult> Delete(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                user.IsDeleted = true;
                await _userManager.UpdateAsync(user);
                return Ok();
            }

            return BadRequest("User could not be found");
        }

        [AllowAnonymous]
        [HttpGet("/api/user/create-roles")]
        public async Task<IActionResult> CreateRoles()
        {
            if(!await _roleManager.RoleExistsAsync("Admin"))
            {
                var role = new Role();
                role.Name = "Admin";
                await _roleManager.CreateAsync(role);
            }

            if (!await _roleManager.RoleExistsAsync("User"))
            {
                var role = new Role();
                role.Name = "User";
                await _roleManager.CreateAsync(role);
            }

            return Ok();
        }
        
    }
}

  public class UserForm
{
    public long Id { get; set; }

    [Required(ErrorMessage = "The {0} field is required.")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "The {0} field is required.")]
    [EmailAddress]
    public string Email { get; set; }

    public string PhoneNumber { get; set; }
    public string Password { get; set; }

    public IList<long> RoleIds { get; set; } = new List<long>();
}
public class RegisterAjaxViewModel
{
    [Required(ErrorMessage = "The {0} field is required.")]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required(ErrorMessage = "The {0} field is required.")]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 4)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }

    [Required(ErrorMessage = "The {0} field is required.")]
    [StringLength(450)]
    public string Name { get; set; }

    [Required(ErrorMessage = "The {0} field is required.")]
    [StringLength(450)]
    public string Surname { get; set; }


    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; }

    public string TimeZoneId { get; set; }

    public Guid UserGuid { get; set; }
}