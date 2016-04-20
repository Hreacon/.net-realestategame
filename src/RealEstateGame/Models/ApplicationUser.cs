using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;

namespace RealEstateGame.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public int UserId { get; set; }
        // current income
        public int Income { get; set; }
        // currently working full time, part time or none
        public string Job { get; set; }
        // What type of dwelling are you living in?
        public string LivingIn { get; set; }
        // the address of the dwelling
        public string Address { get; set; }
        // the amount of rent you have to pay
        public int Rent { get; set; }
        // Current Turn Number
        public int TurnNum { get; set; }
        // current number of action points left until the next turn
        public int Actions { get; set; }
    }

}
