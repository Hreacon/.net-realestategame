using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public ApplicationDbContext context;

        public Player()
        {

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

        public void ImproveHome(int id)
        {
            var home = GetHome(id);
            if (home.Owned == 1 && home.GetCostImprovement() <= Money)
            {
                UseAction();
                Money = Money - home.GetCostImprovement();
                home.Improve();
                SavePlayerAndHome(home);
            }
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
        }

        // Player advances to the next turn
        public void NextTurn()
        {
            // make sure the turn is over
            if (Actions <= 0)
            {
                // replenish action points
                switch (Job)
                {
                    case "Full-Time":
                        Actions = 2;
                        break;
                    case "Part-Time":
                        Actions = 5;
                        break;
                    case "None":
                        Actions = 8;
                        break;
                    default:
                        Actions = 2;
                        break;
                }
                // add income
                Money = Money + Income - Rent;
                // Incriment Turn Number
                TurnNum++;
                if (TurnNum%6 == 0)
                {
                    // every six months, a new home appears!
                    context.Homes.Add(Home.GenerateHome(PlayerId, new Random()));
                    context.SaveChanges();
                }
                if (TurnNum%12 == 0)
                {
                    // every 12 months, homes get re-evaluated
                    Revalue();
                }
            }
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

        public void BuyHome(int id)
        {
            var home = GetHome(id);
            if (home.Asking < Money)
            {
                Money = Money - home.Asking;
                home.Owned = 1;
                home.ForSale = 0;
                SavePlayerAndHome(home);
            }
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
                SavePlayerAndHome(home);
            }
        }

        public void SavePlayerAndHome(Home home)
        {
            context.Update(home);
            Save();
        }
        
        public void Save()
        {
            context.Update(this);
            context.SaveChanges();
        }

        public void SkipTurn()
        {
            Actions = 0;
            NextTurn();
            Save();
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
            return context.Homes.Where(m => m.PlayerId == PlayerId && m.Owned == 1);
        }

        public Home GetHome(int id)
        {
            return context.Homes.FirstOrDefault(m => m.HomeId == id);
        }
    }
}
