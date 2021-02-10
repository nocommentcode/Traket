using System;

namespace Spendings.Controllers
{
    public class ExpenseForm
    {
        public long Id { get; set; }
        public string CategoryName { get; set; }
        public long CategoryId { get; set; }
        public decimal? Long { get; set; }
        public decimal? Lat { get; set; }
        public DateTimeOffset Date { get; set; }
        public decimal Amount { get; set; }
    }
}