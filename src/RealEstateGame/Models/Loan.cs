using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Entity.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateGame.Models
{
    public class Loan
    {
        public int LoanId { get; set; }
        [ForeignKey("Player")]
        public int PlayerId { get; set;} 
        public int Principal { get; set; }
        public double Interest { get; set; }
        public int Term { get; set; }
        public int Payment { get; set; }
    }
}