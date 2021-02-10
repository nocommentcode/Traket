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
    public class BillApiController : Controller
    {
        private IRepository<Bill> _billRepository;
        private IUserService _userService;

        public BillApiController(IRepository<Bill> billRepository,
            IUserService userService)
        {
            _billRepository = billRepository;
            _userService = userService;    
        }

        [HttpPost("/api/bill/create")]
        public async Task<IActionResult> Post([FromBody] BillForm form)
        {
            try
            {
                var bill = new Bill()
                {
                    CreatedOn = DateTime.UtcNow,
                    User = _userService.GetUserFromClaims(HttpContext.User) ?? throw new Exception(),
                    DayOfMonthDebited = form.DayOfMonthDebited,
                    Amount = form.Amount,
                    BillingEnd = form.BillingEnd,
                    BillingStart = form.BillingStart,
                    Name = form.Name
                };
                
                await _billRepository.Add(bill);

                    return Ok(bill);
            }
            catch
            {
                return Unauthorized();
            }
        }

        [HttpGet("/api/bill/list")]
        public async Task<IActionResult> List()
        {
            var user = _userService.GetUserFromClaims(User);
            
            if (user !=null){
                
                var bills = await _billRepository.Query()
                    .Include(x => x.User)
                    .Where(x => x.User.Id == user.Id)
                    .Select(x => new BillForm()
                    { 
                        Id = x.Id, 
                        Name = x.Name,
                        DayOfMonthDebited = x.DayOfMonthDebited,
                        Amount = x.Amount,
                        BillingEnd = x.BillingEnd,
                        BillingStart = x.BillingStart
                    })
                    .ToListAsync();
                
                foreach(var bill in bills)
                {
                    bill.BillingStart = TimeZoneInfo.ConvertTimeFromUtc(bill.BillingStart.UtcDateTime, _userService.GetTimeZone(user));

                    if (bill.BillingEnd.HasValue)
                    {
                        bill.BillingEnd = TimeZoneInfo.ConvertTimeFromUtc(bill.BillingEnd.Value.UtcDateTime, _userService.GetTimeZone(user));
                    }
                }

                return Ok(bills);
            }

            return BadRequest();
        }


        [HttpDelete("/api/bill/delete/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var userId = _userService.GetUserIdFromClaims(User);

            if (userId.HasValue)
            {
                var bill = await _billRepository.Query()
                    .Include(x => x.User)
                    .Where(x => x.User.Id == userId && x.Id == id)
                    .FirstOrDefaultAsync();

                if (bill == null)
                {
                    return NotFound();
                }

                await _billRepository.Delete(bill);

                return Ok(bill);
            }

            return Unauthorized();
        }

        [HttpPut("/api/bill/edit")]
        public async Task<IActionResult> Put([FromBody] BillForm form)
        {
            var userId = _userService.GetUserIdFromClaims(User);

            if (userId.HasValue)
            {
                var bill = await _billRepository.Query()
                    .Include(x => x.User)
                    .Where(x => x.User.Id == userId && x.Id == form.Id)
                    .FirstOrDefaultAsync();

                if (bill== null)
                {
                    return NotFound();
                }

                bill.Name = form.Name;
                bill.DayOfMonthDebited = form.DayOfMonthDebited;
                bill.BillingStart = form.BillingStart;
                bill.BillingEnd = form.BillingEnd;
                bill.Amount = form.Amount;

                await _billRepository.Update(bill);

                return Ok(bill);
            }

            return Unauthorized();
        }

    }
}
