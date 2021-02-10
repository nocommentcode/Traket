using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Spendings.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spendings.Data
{
    public class SpendingsDbContext : IdentityDbContext<User,Role,long, IdentityUserClaim<long>, UserRole, IdentityUserLogin<long>, IdentityRoleClaim<long>, IdentityUserToken<long>>
    {
        public SpendingsDbContext(DbContextOptions<SpendingsDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Expense> Expenses  { get; set; }
        public DbSet<Income> Incomes  { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<UserRefreshToken> UserRefreshToken { get; set; }
    }
}
