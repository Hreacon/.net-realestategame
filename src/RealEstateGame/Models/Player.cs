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
            var country = rand.NextDouble();
            while (country > high) country = rand.NextDouble();
            country = (country - sub)/10;
            var city = rand.NextDouble();
            while (city > high) city = rand.NextDouble();
            city = (city - sub)/10;
            var homes = context.Homes;
            // TODO Add neighborhood 2% as well, but for now just the city/country and home
            foreach (var home in homes)
            {
                var local = (rand.NextDouble() - .3)/10;

                home.Value = (int) Math.Floor(home.Value*(1 + (city + country + local)));
                if (home.Asking > home.Value && home.Owned == 0 && home.ForSale == 1)
                {
                    home.Asking = home.Asking - (home.Asking - home.Value)/2;
                }
                else
                {
                    home.Asking = home.Value;
                }
                context.Homes.Update(home);
            }
            context.SaveChanges();
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

        public bool BuyHome(int homeId)
        {

            var home = context.Homes.FirstOrDefault(m => m.HomeId == homeId);
            // check for money or loan?
            // reduce money
            // own home
            if (Money > home.Asking)
            {
                Money = Money - home.Asking;
                home.ForSale = 0;
                home.Owned = 1;
                context.Homes.Update(home);
                context.Players.Update(this);
                context.SaveChanges();
                return true;
            }
            return false;
        }

        public List<Home> GetOwnedHomes()
        {
            return context.Homes.Where(m => m.PlayerId == PlayerId && m.Owned == 1).ToList();
        }
    }
}
