using Spendings.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Spendings.Services
{
    public interface IExpenseService
    {
        Task<decimal> GetCategoryTotal(long categoryId, DateTime? startDate, DateTime? endDate);
        Task<AmountVsPrevious> GetDaily(User user);
        Task<GraphVm> GetGraph(User user);
        Task<AmountVsPrevious> GetMonthly(User user);
        Task<List<MonthlySummary>> GetMonthlySummaries(User user);
        Task<AmountVsPrevious> GetWeekly(User user);
    }
}