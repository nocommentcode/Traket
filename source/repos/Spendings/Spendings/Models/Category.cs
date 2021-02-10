using Spendings.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Spendings.Models
{
    public class Category : IEntity
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public long Id { get; set; }

        [Required]
        public DateTime DateAdded {get;set;}

        public User User { get; set; }
    }
}
