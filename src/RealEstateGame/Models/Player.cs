using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Data.Entity;

namespace RealEstateGame.Models
{
    // Add profile data for application users by adding properties to the Player class
    public class Player
    {
        [Key]
        public int PlayerId { get; set; }
        public string Username { get; set; }
        [ForeignKey("ApplicationUser")]
        public string UserId { get;set; }
        // current income
        public int Income { get; set; }
        // currently working full time, part time or none
        public string Job { get; set; }
        // What type of dwelling are you living in?
        public string LivingIn { get; set; }
        // the address of the dwelling
        public string Address { get; set; }
        // the amount of rent you have to pay
        public double Rent { get; set; }
        // Current Turn Number
        public int TurnNum { get; set; }
        // current number of action points left until the next turn
        public int Actions { get; set; }
        // current money on hand
        public double Money { get; set; }
        // Current total of Rental Income
        public double RentalIncome { get; set; }
        // Current total of loan payments
        public double LoanPayments { get; set; }
        // Used to reduce useless database calls to save
        public static bool DataChanged = false;

        // DbContext for updating homes when keeping track of turns
        [NotMapped] public ApplicationDbContext context;

        [NotMapped] private List<string> Jobs;

        public Player()
        {
            Jobs = new List<string>();
            Jobs.Add("Full-Time");
            Jobs.Add("Part-Time");
            Jobs.Add("Self Employed");
        }
        
        public void CalculateRentalIncome()
        {
            RentalIncome = 0;
            var renters = context.Renters.Where(m => m.PlayerId == PlayerId && m.Renting == 1).ToList();
            if (renters.Any())
            {
                foreach (var renter in renters)
                {
                    RentalIncome += renter.Rent;
                }
            }
            context.Update(this);
        }
    
        public void CalculateLoanPayments()
        {
            LoanPayments = 0;
            var loans = GetLoans();
            if(loans != null) 
                foreach (var loan in loans)
                {
                    LoanPayments += loan.Payment;
                }
            context.Update(this);
        }

        [NotMapped]
        public double NetPerTurn
        {
            get
            {
                CalculateLoanPayments();
                CalculateRentalIncome();
                return Income - Rent + RentalIncome - LoanPayments;
            }
        }

        public bool IsSelfEmployed()
        {
            return Job == Jobs[2];
        }

        // Player decided to work overtime, give them extra income
        public void WorkOvertime(Random rand)
        {
            double extraPercent = 1;
            extraPercent = rand.NextDouble();
            if (extraPercent > .5) extraPercent = extraPercent/2;
            
            Money = Money + Income*extraPercent;
            UseAction();
        }

        public bool ImproveHome(int id)
        {
            var home = GetHome(id);
            if (home.Owned == 1 && home.GetCostImprovement() <= Money)
            {
                UseAction();
                Money = Money - home.GetCostImprovement();
                home.Improve();
                context.Update(home);
                return true;
            }
            return false;
        }

        // Player has used an action point.
        public void UseAction()
        {
            Actions = Actions - 1;
            if (Actions <= 0)
            {
                // User is out of actions, change turns
                NextTurn();
            }
            context.Update(this);
        }

        // Player advances to the next turn
        public void NextTurn()
        {
            // make sure the turn is over
            if (Actions <= 0)
            {
                // Generate a random number generator
                Random rand = new Random();

                // replenish action points
                if (Job == Jobs[0]) Actions = 2;
                else if (Job == Jobs[1]) Actions = 5;
                else if (Job == Jobs[2]) Actions = 8;
                
                // add income
                CalculateRentalIncome();
                CalculateLoanPayments();
                Money = Money + Income - Rent + RentalIncome;
                
                var homes = GetOwnedHomes();
                foreach (var home in homes)
                {
                    // if home has a loan
                    if (home.loan != null)
                    {
                        // and player can pay for loan
                        if (Money > home.loan.Payment)
                        {
                            if (home.loan.Principal <= home.loan.Payment+1)
                            {
                                // pay only whats left on the mortgage, not the full thing
                                Money = Money - home.loan.Principal;
                                home.loan.MakePayment();
                                var loan = home.loan;
                                home.loan = null;
                                context.Update(home);
                                context.SaveChanges();
                                context.Loans.Remove(loan);
                            }
                            else
                            {
                                Money = Money - home.loan.Payment;
                                home.loan.MakePayment();
                                context.Loans.Update(home.loan);
                            }
                        }
                        else
                        {
                            // TODO player cant pay loans
                        }
                    }

                    // if home has renter
                    if (home.renter != null)
                    {
                        // TODO make renters damage homes randomly
                        // Remember that homes have twice as much chance every 6 mo to lose condition
                        // TODO make renters leave homes that have degraded past the renters allowable limits
                        if (home.renter.StartTurnNum + Renter.Term > TurnNum)
                        {
                            if (Randomly(200, rand) || home.Rent *.9 > home.GetRent())
                            {
                                RemoveRenter(home);
                            }
                        }
                    }
                }

                // not owned homes change for sale status
                foreach (var home in context.Homes.Where(m => m.PlayerId == PlayerId && m.Owned == 0)) // TODO could this be sped up with a for loop? 747ms
                {
                    // home randomly changes for sale status
                    if (Randomly(25, rand))
                    {
                        home.ForSale = home.ForSale == 1 ? 0 : 1;
                        context.Update(home);
                    }
                }

                // Incriment Turn Number
                TurnNum++;
                if (TurnNum%6 == 0)
                {
                    // every six months, a new home appears!
                    context.Homes.Add(Home.GenerateRandomHome(PlayerId, rand));
                    
                    // every six months, chance of home loosing condition point!
                    int chance = 30;
                    foreach (var home in GetOwnedHomes())
                    {
                        if (Randomly(chance, rand))
                        {
                            home.Degrade(rand);
                            context.Homes.Update(home);
                        }
                    }
                }
                if (TurnNum%12 == 0)
                {
                    // every 12 months, homes get re-evaluated
                    Revalue();
                }
            }
        }

        public bool Randomly(int maxvalue, Random rand = null)
        {
            if(rand == null) rand = new Random();
            return rand.Next(0, maxvalue) == maxvalue/2;
        }

        private void Revalue()
        {
            Random rand = new Random();
            // between -.02 and .02
            double high = .4;
            double sub = .2;
            var country = GeneratePercent(sub, high, rand);
            var city = GeneratePercent(sub, high, rand);
            var homes = context.Homes.Where(m=>m.PlayerId == PlayerId).ToList();
            // TODO Add neighborhood 2% as well, but for now just the city/country and home
            foreach (var home in homes)
            {
                if (Randomly(3, rand))
                {
                    home.Revalue(city, country, rand);
                    context.Homes.Update(home);
                }
            }
        }

        public int GetHomeForSaleCount()
        {
            return context.Homes.Count(m => m.PlayerId == PlayerId && m.ForSale == 1);
        }

        public double GeneratePercent(double sub, double high, Random rand)
        {
            double output = 0;
            while (output > high) output = rand.NextDouble();
            output = (output - sub) / 10;
            return output;
        }

        public static Player GeneratePlayer(ApplicationUser user)
        {
            return new Player()
            {
                Username = user.UserName,
                UserId = user.Id,
                Money = 1000.00,
                // TODO: Make the income calculated instead of set
                Income = 1300,
                Job = "Full-Time",
                LivingIn = "Apartment",
                Address = "123 Example St",
                // TODO: Make the rent calculated instead of set
                Rent = 800.00,
                TurnNum = 0,
                Actions = 2,
            };
        }

        public void AddTransaction(Transaction sale)
        {
            context.Transactions.Add(sale);
        }

        public bool BuyHome(int id)
        {
            var home = GetHome(id);
            if (!(home.Asking < Money)) return false;
            Money = Money - home.Asking;
            UseAction();
            home.Owned = 1;
            home.ForSale = 0;
            DataChanged = true;
            AddTransaction(new Transaction(PlayerId, home.HomeId, TurnNum, home.Asking));
            context.Update(home);
            CalculateLoanPayments();
            context.Update(this);
            return true;
        }

        public bool SellHome(int id)
        {
            var home = GetHome(id);
            if (home.Owned == 0) return false;
            if (home.loan != null)
            {
                if (home.loan.Principal > home.Value || home.loan.LoanType == Loan.TYPE_FHA && TurnNum - home.loan.StartTurnNum < Loan.DEFAULT_TERM) return false;    
            }

            if (home.Rented == 1) // home is rented. Get rid of the renter.
            {
                home.Rented = 0;
                home.renter.Renting = 0;
                home.renter.HomeId = 0;
                var renter = home.renter;
                home.renter = null;
                context.Renters.Update(renter);
                context.Update(home);
                CalculateRentalIncome();
                context.Update(this);
            }

            if (Address == home.Address)
            {
                // player lives in house, they sell the home they move into an apartment
                MoveIntoApartment();
            }

            Money = Money + home.Value;

            if (home.loan != null)
            {
                Money = Money - home.loan.Principal;
                var loan = home.loan;
                home.loan = null;
                context.Update(home);
                context.SaveChanges();
                context.Loans.Remove(loan);
            }

            home.Owned = 0;
            home.ForSale = 0;
            home.Asking = home.Value + home.Value/10;
            AddTransaction(new Transaction(PlayerId, home.HomeId, TurnNum, home.Value));
            context.Update(home);
            UseAction();
            // TODO make this more realistic
            return true;
        }

        public void SkipTurn()
        {
            Actions = 0;
            NextTurn();
        }

        public void SetJob(string job)
        {
            if (!Jobs.Contains(job)) return;
            Job = job;
            if (job == Jobs[0])
            {
                Income = 1300;
                if (Actions > 2) Actions = 2;
            }
            else if (job == Jobs[1])
            {
                Income = 700;
                if (Actions > 5) Actions = 5;
            }
            else if (job == Jobs[2]) Income = 0;
            context.Update(this);
        }

        public List<string> GetPotentialJobs()
        {
            List<string> output = new List<string>();
            foreach (var job in Jobs)
            {
                if (job != Job)
                {
                    output.Add(job);
                }
            }
            return output;
        }

        public bool MoveIntoApartment()
        {
            return Move("Apartment", "123 Example St", 800);
        }

        public bool MoveIntoHome(int id)
        {
            var home = GetHome(id);
            if (home.Rented == 1) return false;
            return MoveIntoHome(home);
        }

        public bool MoveIntoHome(Home home)
        {
            return Move("Owned Home", home.Address, 0);
        }

        private bool Move(string livingin, string address, int rent)
        {
            var loans = GetLoans();
            if (loans != null)
            {
                if (loans.Where(loan => loan.LoanType == 1).Any(loan => TurnNum - loan.StartTurnNum < 12))
                {
                    return false;
                }
            }
            LivingIn = livingin;
            Address = address;
            Rent = rent;
            context.Update(this);
            return true;
        }

        public IEnumerable<Home> GetOwnedHomes()
        {
            return context.Homes.Where(m => m.PlayerId == PlayerId && m.Owned == 1).Include(m=>m.renter).Include(m=>m.loan).ToList();
        }

        public Home GetHome(int id)
        {
            return context.Homes.Include(m=>m.loan).Include(m=>m.renter).FirstOrDefault(m => m.HomeId == id);
        }

        // TODO Remove entirely
        public bool HaveContext()
        {
            return true;
        }

        public IEnumerable<Loan> GetLoans()
        {
            var loans = context.Loans.Where(m => m.PlayerId == PlayerId).ToList();
            if (loans.Any()) return loans;
            else return null;
        }

        public void RemoveRenter(Home home)
        {
            var renter = home.renter;

            home.renter = null;
            renter.Renting = 0;
            renter.HomeId = 0;
            home.Rented = 0;
            context.Renters.Update(renter);
            context.Update(home);
        }
    }
}
