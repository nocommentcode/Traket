using Spendings.Models;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Spendings.Services
{
    public interface IUserService
    {
        TimeZoneInfo GetTimeZone(User user);
        User GetUserFromClaims(ClaimsPrincipal principal);
        long? GetUserIdFromClaims(ClaimsPrincipal principal);
        Task SetInitialCategory(User user);
    }
}