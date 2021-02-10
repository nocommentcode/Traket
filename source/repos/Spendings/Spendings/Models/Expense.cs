using Spendings.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spendings.Models
{
    public class Expense : IEntity
    {
        public long Id { get; set; }
        public Category Category { get; set; }
        public DateTimeOffset Date { get; set; }
        public decimal Amount { get; set; }
        public User User { get; set; }
    }
}
