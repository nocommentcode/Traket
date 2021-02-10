using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Spendings.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spendings.Models
{
    public class SpendingRoleStore : RoleStore<Role, SpendingsDbContext, long, UserRole, IdentityRoleClaim<long>>
    {
        public SpendingRoleStore(SpendingsDbContext context) : base(context)
        {
        }
    }
}
