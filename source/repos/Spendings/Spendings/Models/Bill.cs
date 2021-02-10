using Spendings.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Spendings.Models
{
    public class Bill : IEntity
    {
        public long Id { get; set; }
        public DateTimeOffset BillingStart { get; set; }
        public DateTimeOffset? BillingEnd { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public decimal Amount { get; set; }
        public int DayOfMonthDebited { get; set; }
        public string Name { get; set; }
        public User User { get; set; }

    }

    public class BillForm
    {
        public long Id { get; set; }
        public DateTimeOffset BillingStart { get; set; }
        public DateTimeOffset? BillingEnd { get; set; }
        public decimal Amount { get; set; }
        public int DayOfMonthDebited { get; set; }
        public string Name { get; set; }
    }
}
