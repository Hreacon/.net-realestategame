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

        public bool IsSelfEmployed()
        {
            return Job == Jobs[2];
        }

        // Player decided to work overtime, give them extra income
        public void WorkOvertime(Random rand)
        {
            double extraPercent = 1;
            while (extraPercent > .2)
                extraPercent = rand.NextDouble();
            
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
                SavePlayerAndHome(home);
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
            Save();
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
                Money = Money + Income - Rent;

                // pay loans
                var loans = GetLoans();
                foreach (var loan in loans)
                {
                    if (Money > loan.Payment)
                    {
                        loan.MakePayment();
                        Money = Money - loan.Payment;
                        context.Loans.Update(loan);
                    }
                    // TODO else they lose?
                }

                // randomly houses change for sale status
                if (HaveContext())
                {
                    foreach (var home in context.Homes.Where(m => m.PlayerId == PlayerId && m.Owned == 0).ToList())
                    {
                        if (Randomly(250, rand))
                        {
                            home.ForSale = home.ForSale == 1 ? 0 : 1;
                        }
                    }
                }

                // Incriment Turn Number
                TurnNum++;
                if (TurnNum%6 == 0)
                {
                    // every six months, a new home appears!
                    if (HaveContext())
                    {
                        context.Homes.Add(Home.GenerateHome(PlayerId, rand));
                        Save();
                    }


                    // every six months, chance of home loosing condition point!
                    int chance = 10;
                    foreach (var home in GetOwnedHomes())
                    {
                        var inchance = chance;
                        if (home.Rented == 1) inchance = inchance*2;
                        if (Randomly(inchance, rand))
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
                Save();
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
            var homes = context.Homes;
            // TODO Add neighborhood 2% as well, but for now just the city/country and home
            foreach (var home in homes)
            {
                home.Revalue(city, country, rand);
                context.Homes.Update(home);
            }
            context.SaveChanges();
        }

        public int GetHomeForSaleCount()
        {
            return context.Homes.Where(m => m.PlayerId == PlayerId && m.ForSale == 1).ToList().Count;
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

        public void addTransaction(Transaction sale)
        {
            context.Transactions.Add(sale);
        }

        public bool BuyHome(int id)
        {
            var home = GetHome(id);
            if (home.Asking < Money)
            {
                Money = Money - home.Asking;
                home.Owned = 1;
                home.ForSale = 0;
                addTransaction(new Transaction(PlayerId, home.HomeId, TurnNum, home.Asking));
                SavePlayerAndHome(home);
                return true;
            }
            return false;
        }

        public void SellHome(int id)
        {
            var home = GetHome(id);
            if (home.Owned == 1)
            {
                if (Address == home.Address)
                {
                    // player lives in house, they sell the home they move into an apartment
                    MoveIntoApartment();
                }
                Money = Money + home.Value;
                home.Owned = 0;
                home.ForSale = 1;
                home.Asking = home.Value + home.Value/10;
                // TODO make this more realistic
                addTransaction(new Transaction(PlayerId, home.HomeId, TurnNum, home.Value));
                SavePlayerAndHome(home);
            }
        }

        public void SavePlayerAndHome(Home home)
        {
            if (HaveContext())
            {
                context.Update(home);
                Save();
            }
        }
        
        public void Save()
        {
            if (HaveContext())
            {
                context.Update(this);
                context.SaveChanges();
            }
        }

        public void SkipTurn()
        {
            Actions = 0;
            NextTurn();
            Save();
        }

        public void SetJob(string job)
        {
            if (Jobs.Contains(job))
            {
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
            }
            Save();
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

        public void MoveIntoApartment()
        {
            LivingIn = "Apartment";
            Address = "123 Example St";
            Rent = 800;
            Save();
        }

        public void MoveIntoHome(int id)
        {
            var home = GetHome(id);
            LivingIn = "Owned Home";
            Address = home.Address;
            Rent = 0;
            SavePlayerAndHome(home);
        }

        public IEnumerable<Home> GetOwnedHomes()
        {
            if (HaveContext())
            {
                return context.Homes.Where(m => m.PlayerId == PlayerId && m.Owned == 1);
            }
            return null;
        }

        public Home GetHome(int id)
        {
            if (HaveContext())
            {
                return context.Homes.FirstOrDefault(m => m.HomeId == id);
            }
            return null;
        }

        public bool HaveContext()
        {
            return context != null;
        }

        public IEnumerable<Loan> GetLoans()
        {
            return context.Loans.Where(m => m.PlayerId == PlayerId).ToList();
        }
    }
}
