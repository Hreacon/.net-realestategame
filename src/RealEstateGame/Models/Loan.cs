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
        public double Principal { get; set; }
        public double APR { get; set; }
        public int Term { get; set; }
        public double Payment { get; set; }
        public int StartTurnNum { get; set; }
        public int PaymentsLeft { get; set; }
        [NotMapped]
        public virtual Home home { get; set; }

        public Loan(int playerId, double principal, double apr, int term, int startTurnNum)
        {
            PlayerId = playerId;
            Principal = principal;
            APR = apr;
            Term = term;
            StartTurnNum = startTurnNum;

            Payment = Loan.CalculatePayment(principal, apr, term);
            PaymentsLeft = term;
        }

        public Loan()
        {
        }

        public void MakePayment()
        {
            if (Principal > 0)
            {
                ReducePrincipal(Payment - APR/12*Principal);
                PaymentsLeft = PaymentsLeft - 1;
            }
        }

        public void MakeExtraPayment(double amount)
        {
            ReducePrincipal(amount);
        }

        private void ReducePrincipal(double amount)
        {
            Principal = Principal - amount;
            if (Principal <= 0)
            {
                // loan is payed off
                Payment = 0;
                PaymentsLeft = 0;
                Principal = 0;
            }
        }

        public static double CalculatePayment(double principal, double apr, int term)
        {
            return principal * (apr/12 * Math.Pow(1 + apr/12, term)) / (Math.Pow(1 + apr / 12, term) - 1);
        }

        public static double CalculateAffordableAmount(double payment, double apr, int term)
        {
            return payment*(Math.Pow(1 + apr/12, term) - 1)/(apr/12*Math.Pow(1 + apr/12, term));
        }
    }
}