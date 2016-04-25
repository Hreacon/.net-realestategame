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
        private readonly ApplicationDbContext _context;
        private readonly Random _rand;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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
            Player player = null;
            if (_context.Players.Any())
            {
                player = _context.Players.SingleOrDefault(m => m.UserId == userId);
            }
            if (player == null)
            {
                var user = GetUser().Result;
                player = Player.GeneratePlayer(user);
                _context.Players.Add(player);
                _context.SaveChanges();
                player = GetPlayer();
                _context.Homes.AddRange(Home.GenerateHomes(player.PlayerId));
                _context.SaveChanges();
            }
            player.context = _context;
            return player;
        }

        [NonAction]
        private IEnumerable<Home> GetHomes(int playerId)
        {
            if (_context.Homes.Any())
            {

                IEnumerable<Home> homes = _context.Homes.Where(m => m.PlayerId == playerId && m.Owned == 0 && m.ForSale == 1).OrderBy(m => m.Asking);
                if (homes.Any())
                {
                    return homes;
                }
            }
            _context.Homes.AddRange(Home.GenerateHomes(playerId));
            _context.SaveChanges();
            return GetHomes(playerId);
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

        public IActionResult ViewMarket()
        {
            if (!User.IsSignedIn()) return RedirectToAction("Index");
            var player = GetPlayer();
            var homes = GetHomes(player.PlayerId);
            ViewBag.Homes = homes;
            return View(player);
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
            var player = GetPlayer();
            switch (selectedAction)
            {
                case "overtime":
                    player.WorkOvertime(_rand);
                    break;
                case "viewMarket":
                    return RedirectToAction("ViewMarket");
                    break;
                default:
                    break;
            }
            SavePlayer(player);
            return RedirectToAction("Index");
        }


    }
}
