using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spendings.Data;
using Spendings.Models;
using Spendings.Services;

namespace Spendings.Controllers
{

    [Authorize(Roles ="User")]
    [ApiController]
    public class IncomeApiController : Controller
    {
        private IRepository<Income> _incomeRepository;
        private IUserService _userService;
        

        public IncomeApiController(IRepository<Income> incomeRepository,
            IUserService userService)
        {
            _userService = userService;
            _incomeRepository = incomeRepository;
        }

        [HttpPost("/api/income/create")]
        public async Task<IActionResult> Post([FromBody] Income form)
        {
            form.User = _userService.GetUserFromClaims(User);
            
            await _incomeRepository.Add(form);

            return Ok(form);
        }

        [HttpGet("/api/income/list")]
        public async Task<IActionResult> List(DateTime? startDate = null, DateTime? endDate = null)
        {
            var user = _userService.GetUserFromClaims(User);
            
            if (user != null){

                var query = _incomeRepository.Query()
                    .Include(x => x.User)
                    .Where(x => x.User.Id == user.Id);

                if (startDate.HasValue)
                {
                    query = query.Where(x => x.DateReceived >= startDate);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(x => x.DateReceived <= endDate);
                }

                var incomes = await query.ToListAsync();
                
                foreach(var income in incomes)
                {
                    var tz = GetTimeZone(user);
                    income.DateReceived = TimeZoneInfo.ConvertTimeFromUtc(income.DateReceived.UtcDateTime, tz);
                }
                
                return Ok(incomes);
            }

            return BadRequest();
        }


        [HttpDelete("/api/income/delete/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var userId = _userService.GetUserIdFromClaims(User);

            if (userId.HasValue)
            {
                var income = await _incomeRepository.Query()
                    .Include(x => x.User)
                    .Where(x => x.User.Id == userId && x.Id == id)
                    .FirstOrDefaultAsync();

                if (income == null)
                {
                    return NotFound();
                }

                await _incomeRepository.Delete(income);

                return Ok(income);
            }

            return Unauthorized();
        }

        [HttpPut("/api/income/edit")]
        public async Task<IActionResult> Put([FromBody] Income form)
        {
            var userId = _userService.GetUserIdFromClaims(User);

            if (userId.HasValue)
            {
                var income = await _incomeRepository.Query()
                    .Include(x => x.User)
                    .Where(x => x.User.Id == userId && x.Id == form.Id)
                    .FirstOrDefaultAsync();

                if (income == null)
                {
                    return NotFound();
                }

                income.Amount = form.Amount;
                income.DateReceived = form.DateReceived;
                income.Employer = form.Employer;

                await _incomeRepository.Update(income);

                return Ok(income);
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
}
