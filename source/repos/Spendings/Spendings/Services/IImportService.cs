using Microsoft.AspNetCore.Http;
using Spendings.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Spendings.Services
{
    public interface IImportService
    {
        Task<List<string>> ImportExpenses(IFormFile file, User user);
    }
}