using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Mvc;
using RealEstateGame.Models;

namespace RealEstateGame.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private ApplicationDbContext _context;
        private Random _rand;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager )
        {
            _rand = new Random();
            _context = context;
            _userManager = userManager;
        }

        // Helper functions
        [NonAction]
        private async Task<ApplicationUser> GetUser()
        {
            ApplicationUser u = await _userManager.FindByIdAsync(User.GetUserId());
            return u;
        }
        [NonAction]
        private Player GetPlayer()
        {
            if (!User.IsSignedIn()) return null;
            var userId = User.GetUserId();
            Player player;
            if (_context.Players.Any())
            {
                player = _context.Players.First(m => m.UserId == userId);
            } else { 
                var user = GetUser().Result;
                player = new Player()
                {
                    Username = user.UserName,
                    UserId = User.GetUserId(),
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
                _context.Players.Add(player);
                _context.SaveChanges();
                player = GetPlayer();
            }
            return player;
        }

        [NonAction]
        private void SavePlayer(Player currentPlayer)
        {
            _context.Update(currentPlayer);
            _context.SaveChanges();
        }

        // Routes

        public IActionResult Index()
        {
            if (User.IsSignedIn())
                return View("MainPage", GetPlayer());
            else 
            return View();
        }
        
        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Action(string selectedAction)
        {
            var user = GetPlayer();
            switch (selectedAction)
            {
                case "overtime":
                    user.WorkOvertime(_rand);
                    break;
                default:
                    break;
            }
            SavePlayer(user);
            return RedirectToAction("Index");
        }

        
    }
}
