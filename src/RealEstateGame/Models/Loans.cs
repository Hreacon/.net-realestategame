using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Entity.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateGame.Models
{
    public class Loans
    {
        public int LoanId { get; set; }
        [ForeignKey("ApplicationUser")]
        public string UserId { get; set;} 
        public int Principal { get; set; }
        public double Interest { get; set; }
        public int Term { get; set; }
        public int Payment { get; set; }
    }
}