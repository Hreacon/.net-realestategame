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
        [ForeignKey("Home")]
        public int HomeId { get; set; }
        public double Principal { get; set; }
        public int Term { get; set; }
        public double Payment { get; set; }
        public int StartTurnNum { get; set; }
        public int PaymentsLeft { get; set; }
        public int LoanType { get; set; } // 0 for regular, 1 for fha

        public static int TYPE_REGULAR = 0;
        public static int TYPE_FHA = 1;
        public static int DEFAULT_TERM = 12;

        [NotMapped] public static double APR = .035;
        [NotMapped] public static double FHAAPR = APR + .03;
        [NotMapped] public static int FHACondition = 7;

        public Loan(int playerId, double principal, double apr, int term, int startTurnNum, Home inhome, int loanType = 0)
        {
            PlayerId = playerId;
            Principal = principal;
            APR = apr;
            Term = term;
            StartTurnNum = startTurnNum;
            HomeId = inhome.HomeId;
            LoanType = loanType; // default regular

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