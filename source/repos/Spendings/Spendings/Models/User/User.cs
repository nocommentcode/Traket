using Microsoft.AspNetCore.Identity;
using Spendings.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Spendings.Models
{
    public class User : IdentityUser<long>, IEntity
    {
        [StringLength(450)]
        public string RefreshTokenHash { get; set; }
        public Guid UserGuid { get; set; }
        public bool IsDeleted { get; set; }
        public IList<UserRole> Roles { get; set; } = new List<UserRole>();

        [StringLength(450)]
        public string Name { get; set; }

        [StringLength(450)]
        public string Surname { get; set; }
        public string TimeZoneId { get; set; }
        public List<UserRefreshToken> RefreshTokens { get; set; } = new List<UserRefreshToken>();

    }

    public class UserRefreshToken : IEntity
    {
        public User User { get; set; }
        public string RefreshTokenHash { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateExpires { get; set; }
        public long Id { get ; set; }
    }
}
