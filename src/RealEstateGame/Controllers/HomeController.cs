using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
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
                player = Player.GeneratePlayer(user);
                _context.Players.Add(player);
                _context.SaveChanges();
                player = GetPlayer();
                _context.Homes.AddRange(Home.GenerateHomes(player.PlayerId));
                // TODO Generate renters
                _context.SaveChanges();
            }
            player.context = _context;
            return player;
        }

        [NonAction]
        private IEnumerable<Home> GetHomesForSale(int playerId)
        {
            if (_context.Homes.Any())
            {
                
                IEnumerable<Home> homes = _context.Homes.Where(m => m.PlayerId == playerId && m.Owned == 0 && m.ForSale == 1).OrderBy(m=>m.Asking);
                if (homes.Count() > 0)
                {
                    return homes;
                }
            }
            _context.Homes.AddRange(Home.GenerateHomes(playerId));
            _context.SaveChanges();
            return GetHomesForSale(playerId);
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
            var homes = GetHomesForSale(player.PlayerId);
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

        public IActionResult BuyHome(int id)
        {
            GetPlayer().BuyHome(id);
            return RedirectToAction("ViewMarket");
        }

        public IActionResult SellHome(int id)
        {
            GetPlayer().SellHome(id);
            return RedirectToAction("Index");
        }

        public IActionResult Move()
        {
            var player = GetPlayer();
            ViewBag.Homes = player.GetOwnedHomes();
            return View(player);
        }

        [HttpPost]
        public IActionResult Move(FormCollection collection)
        {
            var desc = Request.Form["Description"];
            int HomeId = 0;
            Int32.TryParse(Request.Form["homeId"], out HomeId);
            if (HomeId > 0)
            {
                GetPlayer().MoveIntoHome(HomeId);
            }
            else
            {
                GetPlayer().MoveIntoApartment();
            }
            return RedirectToAction("Index");
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
                case "moving":
                    return RedirectToAction("Move");
                default:
                    break;
            }
            player.Save();
            return RedirectToAction("Index");
        }

        
    }
}
