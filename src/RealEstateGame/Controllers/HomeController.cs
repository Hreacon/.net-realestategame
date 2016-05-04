using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authorization;
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
        private Player GetPlayer(bool makeNew = false)
        {
            if (!User.IsSignedIn()) return null;
            var userId = User.GetUserId();
            Player player;
            if (_context.Players.Any() && !makeNew)
            {
                try
                {
                    player = _context.Players.First(m => m.UserId == userId);
                }
                catch
                {
                    return GetPlayer(true);
                }
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
        [Authorize(Roles = "Player")]
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
            {
                ViewData["partial"] = "MainControls";
                ViewBag.Player = GetPlayer();
                return View("MainPage");
            }
            return View();
        }

        [Authorize(Roles = "Player")]
        public IActionResult ViewMarket(string ajax)
        {
            var player = GetPlayer();
            var homes = GetHomesForSale(player.PlayerId);
            if (ajax == "true")
            {
                return View("MarketPartial", homes);
            }
            ViewBag.Player = player;
            ViewData["partial"] = "MarketPartial";
            return View("MainPage", homes);
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

        [Authorize(Roles = "Player")]
        public IActionResult BuyHome(int id)
        {
            GetPlayer().BuyHome(id);
            return RedirectToAction("ViewMarket");
        }

        [Authorize(Roles = "Player")]
        public IActionResult SellHome(int id)
        {
            GetPlayer().SellHome(id);
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Player")]
        public IActionResult Move()
        {
            var player = GetPlayer();
            ViewBag.Homes = player.GetOwnedHomes();
            return View(player);
        }

        [Authorize(Roles = "Player")]
        public IActionResult Improve()
        {
            var player = GetPlayer();
            ViewBag.Homes = player.GetOwnedHomes();
            return View(player);
        }

        [HttpPost]
        [Authorize(Roles = "Player")]
        public IActionResult Improve(FormCollection collection)
        {
            var player = GetPlayer();
            player.ImproveHome(Int32.Parse(Request.Form["homeId"]));
            return RedirectToAction("Improve");
        }

        [HttpPost]
        [Authorize(Roles = "Player")]
        public IActionResult Move(FormCollection collection)
        {
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

        [Authorize(Roles = "Player")]
        public IActionResult ManageJobs(string ajax)
        {
            if (ajax == "true")
            {
                return View("ManageJobsPartial", GetPlayer());
            }
            return View(GetPlayer());
        }

        [HttpPost]
        public IActionResult ManageJobs(FormCollection collection)
        { 
            GetPlayer().SetJob(Request.Form["newJob"]);
            return RedirectToAction("Index");
        }


        [HttpPost]
        [Authorize(Roles = "Player")]
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
                    break;
                case "improve":
                    return RedirectToAction("Improve");
                    break;
                case "skipturn":
                    GetPlayer().SkipTurn();
                    return RedirectToAction("Index");
                case "managejob":
                    return RedirectToAction("ManageJobs");
                default:
                    break;
            }
            return RedirectToAction("Index");
        }

        
    }
}
