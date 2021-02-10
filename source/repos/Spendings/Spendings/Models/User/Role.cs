using Microsoft.AspNetCore.Identity;
using Spendings.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spendings.Models
{
    public class Role : IdentityRole<long>, IEntity
    {
    }
}
