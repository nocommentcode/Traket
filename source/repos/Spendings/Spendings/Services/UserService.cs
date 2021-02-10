using Spendings.Data;
using Spendings.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Spendings.Services
{
    public class UserService : IUserService
    {
        private IRepository<User> _userRepository;
        private IRepository<Category> _categoryRepository;

        public UserService(IRepository<User> userRepository, IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;
            _userRepository = userRepository;
        }

        public User GetUserFromClaims(ClaimsPrincipal principal)
        {
            if (long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out long userId))
            {
                return _userRepository.Query().FirstOrDefault(x => x.Id == userId);
            }
            else
            {
                return null;
            }
        }
        public long? GetUserIdFromClaims(ClaimsPrincipal principal)
        {
            if (long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out long userId))
            {
                return userId;
            }
            else
            {
                return null;
            }
        }

        public TimeZoneInfo GetTimeZone(User user)
        {
            TimeZoneInfo tz;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time");
            }
            return tz;
        }

        public async Task SetInitialCategory(User user)
        {
            await _categoryRepository.Add(new Category()
            {
                User = user,
                DateAdded = DateTime.UtcNow,
                Name = "Other"
            });
        }
    }
}
