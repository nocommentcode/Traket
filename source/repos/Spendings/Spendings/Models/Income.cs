using Spendings.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spendings.Models
{
    public class Income : IEntity
    {
        public long Id { get; set; }
        public decimal Amount { get; set; }
        public DateTimeOffset DateReceived { get; set; }
        public string Employer { get; set; }
        public User User { get; set; }

    }
}
