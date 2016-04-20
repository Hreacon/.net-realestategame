using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateGame.Models
{
    public class Loans
    {
        public int LoanId { get; set; }
        public int Principal { get; set; }
        public double Interest { get; set; }
        public int Term { get; set; }
        public int Payment { get; set; }
        public int UserId { get; set;} 
    }
}