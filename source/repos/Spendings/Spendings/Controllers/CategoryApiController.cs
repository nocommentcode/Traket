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
    public class CategoryApiController : Controller
    {
        private IRepository<Category> _categoryRepository;
        private IUserService _userService;
        private IExpenseService _expenseService;

        public CategoryApiController(IRepository<Category> categoryRepository,
            IUserService userService,
            IExpenseService expenseService)
        {
            _expenseService = expenseService;
            _userService = userService;
            _categoryRepository = categoryRepository;
        }

        [HttpPost("/api/category/create")]
        public async Task<IActionResult> Post([FromBody] CategoryForm form)
        {
            var category = new Category()
            {
                Name = form.Name,
                DateAdded = DateTime.Now,
                User = _userService.GetUserFromClaims(User)
            };
            
            await _categoryRepository.Add(category);

            return Ok(category);
        }

        [HttpGet("/api/category/list")]
        public async Task<IActionResult> List(DateTime? startDate = null, DateTime? endDate = null)
        {
            var userId = _userService.GetUserIdFromClaims(User);
            
            if (userId.HasValue){
                
                var categories = await _categoryRepository.Query()
                    .Include(x => x.User)
                    .Where(x => x.User.Id == userId)
                    .Select(x => new CategoryForm()
                    { 
                        Id = x.Id, 
                        Name = x.Name
                    })
                    .ToListAsync();
                
                foreach(var category in categories)
                {
                    category.Total = await _expenseService.GetCategoryTotal(category.Id, startDate, endDate);
                }

                return Ok(categories);
            }

            return BadRequest();
        }


        [HttpDelete("/api/category/delete/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var userId = _userService.GetUserIdFromClaims(User);

            if (userId.HasValue)
            {
                var category = await _categoryRepository.Query()
                    .Include(x => x.User)
                    .Where(x => x.User.Id == userId && x.Id == id)
                    .FirstOrDefaultAsync();

                if (category == null)
                {
                    return NotFound();
                }

                await _categoryRepository.Delete(category);

                return Ok(category);
            }

            return Unauthorized();
        }

        [HttpPut("/api/category/edit")]
        public async Task<IActionResult> Put([FromBody] CategoryForm form)
        {
            var userId = _userService.GetUserIdFromClaims(User);

            if (userId.HasValue)
            {
                var category = await _categoryRepository.Query()
                    .Include(x => x.User)
                    .Where(x => x.User.Id == userId && x.Id == form.Id)
                    .FirstOrDefaultAsync();

                if (category== null)
                {
                    return NotFound();
                }

                category.Name = form.Name;

                await _categoryRepository.Update(category);

                return Ok(category);
            }

            return Unauthorized();
        }

    }
}
