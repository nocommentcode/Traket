using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spendings.Data;
using Spendings.Models;
using Spendings.Services;

namespace Spendings.Controllers
{
    [Route("expenses")]
    public class ExpenseController : Controller
    {
        private IRepository<Expense> _expenseRepository;
        private IRepository<Category> _categoryRepository;
        private IUserService _userService;
        private IImportService _importService;
        private IExpenseService _expenseService;

        public ExpenseController(IRepository<Expense> expenseRepository,
            IUserService userService,
            IRepository<Category> categoryRepository,
            IExpenseService expenseService,
            IImportService importService)
        {
            _expenseService = expenseService;
            _importService = importService;
            _categoryRepository = categoryRepository;
            _expenseRepository = expenseRepository;
            _userService = userService;
        }

        [HttpGet("/monthly-summary")]
        public async Task<IActionResult> GetMonthylSummary()
        {
            var user = _userService.GetUserFromClaims(User);

            if (user != null)
            {
                var results = await _expenseService.GetMonthlySummaries(user);
                return Ok(results);
            }
            return Unauthorized();
        }


        [HttpPost("/import-expenses")]
        public async Task<IActionResult> ImportPost (IFormFile file)
        {
            var user = _userService.GetUserFromClaims(User);

            if (user != null)
            {
                var results = await _importService.ImportExpenses(file, user);
                return Ok(results);
            }
            return Unauthorized();
        }

        [HttpPost("/new-expense")]
        public async Task<IActionResult> Post([FromBody] ExpenseForm form)
        {  
            var userId = _userService.GetUserIdFromClaims(User);

            if (userId.HasValue)
            {
                var category = await _categoryRepository.Query()
                      .Include(x => x.User)
                      .Where(x => x.User.Id == userId && x.Id == form.CategoryId)
                      .FirstOrDefaultAsync();


                var expense = new Expense()
                {
                    Amount = form.Amount,
                    Date = form.Date,
                    User = _userService.GetUserFromClaims(User),
                    Category = category
                };

                await _expenseRepository.Add(expense);

                return Ok(expense);
            }
            return Unauthorized();
        }

        [HttpPut("/edit-expense")]
        public async Task<IActionResult> Put([FromBody] ExpenseForm form)
        {
            var userId = _userService.GetUserIdFromClaims(User);

            if (userId.HasValue)
            {
                var expense = await _expenseRepository.Query()
                    .Include(x => x.User)
                    .Include(x => x.Category)
                    .Where(x => x.User.Id == userId && x.Id == form.Id)
                    .FirstOrDefaultAsync();

                if (expense == null)
                {
                    NotFound();
                }

                expense.Amount = form.Amount;
                expense.Date = form.Date;

                if (expense.Category.Id != form.CategoryId)
                {
                    var category = await _categoryRepository.Query()
                       .Include(x => x.User)
                       .Where(x => x.User.Id == userId && x.Id == form.CategoryId)
                       .FirstOrDefaultAsync();

                    if (category != null)
                    {
                        expense.Category = category;
                    }
                }

                await _expenseRepository.Update(expense);

                return Ok(expense);
            }

            return Unauthorized();
        }
    

        [HttpDelete("/delete-expense/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var userId = _userService.GetUserIdFromClaims(User);

            if (userId.HasValue)
            {
                var expense = await _expenseRepository.Query()
                    .Include(x => x.User)
                    .Where(x => x.User.Id == userId && x.Id == id)
                    .FirstOrDefaultAsync();

                if (expense == null)
                {
                    NotFound();
                }

                await _expenseRepository.Delete(expense);

                return Ok(expense);
            }

            return Unauthorized();
        }

        [HttpGet("/list-expenses")]
        public async Task<IActionResult> List(DateTime? startDate=null, DateTime? endDate=null)
        {
            var user = _userService.GetUserFromClaims(User);

            if (user != null)
            {

                var query =  _expenseRepository.Query()
                    .Include(x => x.User)
                    .Include(x => x.Category)
                    .Where(x => x.User.Id == user.Id);

                if (startDate.HasValue)
                {
                    query = query.Where(x => x.Date >= startDate);
                }
                if (endDate.HasValue)
                {
                    query = query.Where(x => x.Date <= endDate);
                }

                var expenses = await query.Select(x => new ExpenseForm() 
                    {
                        Amount = x.Amount,
                        CategoryName = x.Category.Name,
                        Date = x.Date,
                        Id = x.Id,
                        CategoryId = x.Category.Id
                    }
                )
                .OrderByDescending(x => x.Date)
                .ToListAsync();

                // convert to local time
                foreach(var expense in expenses)
                {
                    expense.Date = TimeZoneInfo.ConvertTimeFromUtc(expense.Date.UtcDateTime, GetTimeZone(user));
                }

                return Ok(expenses);
            }

            return Unauthorized();
        }

        [HttpGet("/expense-graph")]
        public async Task<IActionResult> GetGraph()
        {
            var user = _userService.GetUserFromClaims(User);

            if (user != null)
            {
                var data = await _expenseService.GetGraph(user);
                return Ok(data);
            }
            return Unauthorized();
        }

        [HttpGet("expense-quick-info")]
        public async Task<IActionResult> QuickInfo()
        {
            var user = _userService.GetUserFromClaims(User);

            if (user != null)
            {
                var daily = await _expenseService.GetDaily(user);
                var weekly = await _expenseService.GetWeekly(user);
                var monthly = await _expenseService.GetMonthly(user);
                var model = new ExpensesQuickInfo()
                {
                    SpendingsToday = daily.Current,
                    SpendingsTodayVsLastWeek = daily.Previous,
                    SpendingsWTD = weekly.Current,
                    SpendingsWTDVsLastWeek = weekly.Previous,
                    SpendingsMTD = monthly.Current,
                    SpendingsMTDVsLastMonth = monthly.Previous
                };

                return Ok(model);
            }
            return Unauthorized();
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

    public class ExpensesQuickInfo
    {
        public decimal SpendingsToday { get; set; }
        public decimal SpendingsTodayVsLastWeek{ get; set; }
        public decimal SpendingsWTD { get; set; }
        public decimal SpendingsWTDVsLastWeek { get; set; }
        public decimal SpendingsMTD { get; set; }
        public decimal SpendingsMTDVsLastMonth { get; set; }
    }
}
