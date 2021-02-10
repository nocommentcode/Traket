using Microsoft.EntityFrameworkCore;
using Spendings.Data;
using Spendings.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spendings.Services
{
    public class ExpenseService : IExpenseService
    {
        private IRepository<Expense> _expenseRepository;
        private IRepository<Income> _incomeRepository;
        private IRepository<Bill> _billRepository;

        public ExpenseService(IRepository<Expense> expenseRepository,
            IRepository<Income> incomeRepository,
            IRepository<Bill> billRepository)
        {
            _billRepository = billRepository;
            _incomeRepository = incomeRepository;
            _expenseRepository = expenseRepository;
        }

        
        public async Task<List<MonthlySummary>> GetMonthlySummaries(User user)
        {
            var firstExpense = await _expenseRepository.Query()
                .Include(x => x.User)
                .Where(x => x.User.Id == user.Id)
                .Select(x => x.Date)
                .OrderBy(x => x.Date)
                .Take(1)
                .FirstOrDefaultAsync();

            var tz = GetTimeZone(user);
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
            var firstDate = TimeZoneInfo.ConvertTimeFromUtc(firstExpense.Date, tz).Date;
            var months = new List<MonthlySummary>();
            while(firstDate < today)
            {
                var startDate = TimeZoneInfo.ConvertTimeToUtc(new DateTime(firstDate.Year, firstDate.Month, 1), tz);
                var endDate = startDate.AddMonths(1);

                months.Add(new MonthlySummary()
                {
                    Year = firstDate.Year,
                    Month = firstDate.Month,
                    TotalExpenses = await GetTotalExpense(user, startDate, endDate),
                    TotalIncome = await GetTotalIncome(user,startDate, endDate),
                    TotalBills = await GetTotalBills(user,startDate, endDate, firstDate.Month == today.Month ? today.Day : 31)
                });

                firstDate = firstDate.AddMonths(1);
            }

            return months;
        }

        public async Task<decimal> GetCategoryTotal(long categoryId, DateTime? startDate, DateTime? endDate)
        {
            var query = _expenseRepository.Query()
                .Include(x => x.Category)
                .Where(x => x.Category.Id == categoryId);

            if (startDate.HasValue)
            {
                query = query.Where(x => x.Date >= startDate);
            } 
            if (endDate.HasValue)
            {
                query = query.Where(x => x.Date <= endDate);
            }
                                
            return await query.Select(x => x.Amount)
                .SumAsync();
        }
        public async Task<GraphVm> GetGraph(User user)
        {
            var today = MidnightLocalInUTC(user);
            var todayLocal = MidnightLocal(user);

            var graphVm = new GraphVm();
            
            for (var i =1; i< 32; i++)
            {
                if (i <= todayLocal.Day)
                {
                    graphVm.Days.Add(i);
                    var startDate = today.AddDays(-(todayLocal.Day - i));
                    var endDate = startDate.AddDays(1);
                    var amount = await _expenseRepository.Query()
                        .Include(x => x.User)
                        .Where(x => x.User.Id == user.Id)
                        .Where(x => x.Date >= startDate && x.Date <= endDate)
                        .Select(x => x.Amount)
                        .SumAsync();
                    var previous = i == 1 ? 0 : graphVm.Actual.Last();
                    graphVm.Actual.Add(amount + previous);
                }
                else
                {
                    graphVm.Days.Add(i);
                    graphVm.Actual.Add(graphVm.Actual.Last());
                }
            }

            var perDay = graphVm.Actual.Last() / todayLocal.Day;
            foreach(var day in graphVm.Days)
            {
                graphVm.Predicted.Add(perDay * day);
            }

            return graphVm;
        }
        public async Task<AmountVsPrevious> GetDaily(User user)
        {
            var today = MidnightLocalInUTC(user);

            return await GetAmount(user, today, today.AddDays(1), today.AddDays(-7), today.AddDays(-6));
        }
        public async Task<AmountVsPrevious> GetWeekly(User user)
        {
            var today = MidnightLocalInUTC(user);

            var todayLocal = MidnightLocal(user);

            int daysToLastMonday;
            var daysToLastSunday = (int)todayLocal.DayOfWeek;
            if (daysToLastSunday == 0)
            {
                daysToLastMonday = 6;
            }
            else
            {
                daysToLastMonday = daysToLastSunday - 1;
            }

            return await GetAmount(user,
                today.AddDays(-daysToLastMonday), 
                today.AddDays(1),
                today.AddDays(-(daysToLastMonday + 7)),
                today.AddDays(-7 + 1));
        }
        public async Task<AmountVsPrevious> GetMonthly(User user)
        {
            var today = MidnightLocalInUTC(user);


            var todayLocal = MidnightLocal(user);


            int daysToFirstOfMonth = todayLocal.Day - 1;

            return await GetAmount(user,
                today.AddDays(-daysToFirstOfMonth), 
                today.AddDays(1), 
                today.AddMonths(-1).AddDays(-daysToFirstOfMonth), 
                today.AddMonths(-1).AddDays(1));
        }

        private async Task<AmountVsPrevious> GetAmount(User user, DateTime currentStart, DateTime currentEnd, DateTime comparisonStart, DateTime comparisonEnd)
        {
            try
            {
                var aVP = new AmountVsPrevious()
                {
                    Current = await GetTotalExpense(user, currentStart, currentEnd)
                };
                try
                {
                    var prevTotal = await GetTotalExpense(user, comparisonStart, comparisonEnd);
                    aVP.Previous = ((aVP.Current - prevTotal) / prevTotal) * 100;

                    return aVP;
                }
                catch 
                {
                    aVP.Previous = 0;
                    return aVP; 
                }
             
            }
            catch
            {
                return new AmountVsPrevious() { Current = 0, Previous = 0 };
            }
            
        }
        private async Task<decimal> GetTotalExpense(User user, DateTime timeSpanStart, DateTime timeSpanEnd)
        {

            return await _expenseRepository.Query()
                .Include(x => x.User)
                .Where(x => x.User.Id == user.Id)
                .Where(x => x.Date >= timeSpanStart && x.Date <= timeSpanEnd)
                .Select(x => x.Amount)
                .SumAsync();

        }
        private async Task<decimal> GetTotalIncome(User user, DateTime timeSpanStart, DateTime timeSpanEnd)
        {

            return await _incomeRepository.Query()
                .Include(x => x.User)
                .Where(x => x.User.Id == user.Id)
                .Where(x => x.DateReceived >= timeSpanStart && x.DateReceived <= timeSpanEnd)
                .Select(x => x.Amount)
                .SumAsync();

        }
        private async Task<decimal> GetTotalBills(User user, DateTime timeSpanStart, DateTime timeSpanEnd, int localDay)
        {

            return await _billRepository.Query()
                .Include(x => x.User)
                .Where(x => x.User.Id == user.Id)
                .Where(x => x.BillingStart <= timeSpanStart && (x.BillingEnd == null || x.BillingEnd >= timeSpanStart) && x.BillingStart <= timeSpanEnd &&  x.DayOfMonthDebited <= localDay)
                .Select(x => x.Amount)
                .SumAsync();

        }

        private DateTime MidnightLocalInUTC(User user)
        {
            var tz = GetTimeZone(user);
            var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
            return TimeZoneInfo.ConvertTimeToUtc(todayLocal, tz);

        }
        private DateTime MidnightLocal(User user)
        {
            var tz = GetTimeZone(user);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
        }

        private TimeZoneInfo GetTimeZone(User user)
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


    }

    public class AmountVsPrevious
    {
        public decimal Current { get; set; }
        public decimal Previous { get; set; }
    }

    public class GraphVm
    {
        public List<int> Days { get; set; } = new List<int>();
        public List<decimal> Actual { get; set; } = new List<decimal>();
        public List<decimal> Predicted { get; set; } = new List<decimal>();
    }

    public class MonthlySummary
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalBills { get; set; }
    }


}
