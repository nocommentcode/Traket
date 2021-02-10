using CsvHelper;
using Microsoft.AspNetCore.Http;
using Spendings.Data;
using Spendings.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Spendings.Services
{
    public class ImportService : IImportService
    {
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Expense> _expenseRepository;

        public ImportService(IRepository<Category> categoryRepository, IRepository<Expense> expenseRepository)
        {
            _categoryRepository = categoryRepository;
            _expenseRepository = expenseRepository;
        }

        public async Task<List<string>> ImportExpenses(IFormFile file, User user)
        {
            if (file == null || file.Length == 0 || !Path.GetExtension(file.FileName).Contains(".csv"))
            {
                throw new Exception("File is invalid");
            }

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var expenses = csv.GetRecords<ExpenseImport>();
                    var results = new List<string>();

                    foreach (var import in expenses)
                    {
                        try
                        {
                            var expense = new Expense()
                            {
                                Date = DateTime.ParseExact(import.Date,"d/MM/yyyy", CultureInfo.InvariantCulture),
                                Amount = import.Amount,
                                User = user,
                                Category = await GetCategory(import.Category, user)
                            };
                            await _expenseRepository.Add(expense);
                            results.Add($"Added Expense ({expense.Id}) to category ({expense.Category.Name})");
                        }
                        catch
                        {
                            results.Add($"Error");
                        }
                    }
                    return results;
                }
            }
        }

        private async Task<Category> GetCategory(string category, User user)
        {
            var cat = _categoryRepository.Query().FirstOrDefault(x => x.Name == category);
            if (cat == null)
            {
                cat = new Category()
                {
                    Name = category,
                    User = user,
                    DateAdded = DateTime.Now
                };
                await _categoryRepository.Add(cat);
            }

            return cat;
        }
    }

    public class ExpenseImport
    {
        public string Date { get; set; }
        public string Category { get; set; }
        public decimal Amount { get; set; }
    }
}
