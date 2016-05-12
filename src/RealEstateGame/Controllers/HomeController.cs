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
            while (true)
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
            }
        }

        // Routes

        public IActionResult Index(string ajax)
        {
            ViewBag.Payemnt = Loan.CalculatePayment(100000, .045, 360);
            ViewBag.Principal = Loan.CalculateAffordableAmount(1300, .045, 360);
            if (User.IsSignedIn())
            {
                ViewBag.Player = GetPlayer();
                if (ajax == "true")
                {
                    return PartialView("MainControls");
                }
                ViewData["partial"] = "MainControls";
                return View("MainPage");
            }
            return View();
        }

        [Authorize(Roles = "Player")]
        public IActionResult GetPlayer(string ajax)
        {
            if (ajax == "true")
            {
                ViewBag.Player = GetPlayer();
                return PartialView("DisplayTemplates/view-player");
            }
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Player")]
        public IActionResult ViewMarket(string ajax)
        {
            var player = GetPlayer();
            ViewBag.Homes = GetHomesForSale(player.PlayerId);
            ViewBag.Player = player;
            if (ajax == "true")
            {
                return PartialView("MarketPartial");
            }
            ViewData["partial"] = "MarketPartial";
            return View("MainPage");
        }

        [HttpPost]
        [Authorize(Roles = "Player")]
        public IActionResult Buy(FormCollection collection)
        {
            var id = 0;
            Int32.TryParse(Request.Form["homeId"], out id);
            if (id > 0)
            {
                var ajax = Request.Form["ajax"].ToString();
                var player = GetPlayer(); 
                var success = player.BuyHome(id);
                if (success)
                {
                    ViewBag.Homes = GetHomesForSale(player.PlayerId);
                    if (ajax == "true")
                    {
                        return PartialView("MarketPartial");
                    }
                }
                else
                {
                    return Content("You Can't Afford This Home");
                }
            }
            return RedirectToAction("ViewMarket");
        }

        [Authorize(Roles = "Player")]
        public IActionResult SellHome(int id)
        {
            GetPlayer().SellHome(id);
            return RedirectToAction("Index");
        }

        // Replace move and improve with Portfolio
        [Authorize(Roles = "Player")]
        public IActionResult Portfolio(string ajax)
        {
            ViewBag.Player = GetPlayer();
            ViewData["partial"] = "PortfolioPartial";
            if (ajax == "true")
            {
                return PartialView(ViewData["partial"].ToString());
            }
            return View("MainPage");
        }

        [HttpPost]
        [Authorize(Roles = "Player")]
        public IActionResult Improve(FormCollection collection)
        {
            var player = GetPlayer();
            var ajax = Request.Form["ajax"].ToString();
            var id = Int32.Parse(Request.Form["homeId"].ToString());
            var success = player.ImproveHome(id);
            if (success)
            {
                ViewBag.Player = player;
                if (ajax == "true")
                {
                    return PartialView("PortfolioPartial");
                }
                return RedirectToAction("Portfolio");
            }
            return Content("You can't afford to improve this home...");
        }

        [HttpPost]
        [Authorize(Roles = "Player")]
        public IActionResult Move(FormCollection collection)
        {
            int HomeId = 0;
            Int32.TryParse(Request.Form["homeId"], out HomeId);
            ViewBag.Player = GetPlayer();
            string errorMessage = "You can't move right now. FHA Loan Restrictions";
            if (HomeId > 0)
            {
                if (!ViewBag.Player.MoveIntoHome(HomeId))
                    return Content(errorMessage);
            }
            else
            {
                if (!ViewBag.Player.MoveIntoApartment())
                    return Content(errorMessage);
            }
            if (Request.Form["ajax"].ToString() == "true")
            {
                return PartialView("PortfolioPartial");
            }
            return RedirectToAction("Portfolio");
        }

        [HttpPost]
        [Authorize(Roles = "Player")]
        public IActionResult Sell(FormCollection collection)
        {
            ViewBag.Player = GetPlayer();
            if (!ViewBag.Player.SellHome(Int32.Parse(Request.Form["homeId"])))
                return Content("You can't sell this home right now");
            if (Request.Form["ajax"].ToString() == "true")
            {
                return PartialView("PortfolioPartial");
            }
            return RedirectToAction("Portfolio");
        }

        [Authorize(Roles = "Player")]
        public IActionResult ManageJobs(string ajax)
        {
            ViewBag.Player = GetPlayer();
            ViewData["partial"] = "ManageJobsPartial";
            if (ajax == "true")
            {
                return PartialView(ViewData["partial"].ToString());
            }
            return View("MainPage");
        }

        [HttpPost]
        public IActionResult ManageJobs(FormCollection collection)
        { 
            ViewBag.Player = GetPlayer();
            ViewBag.Player.SetJob(Request.Form["newJob"]);
            if (Request.Form["ajax"].ToString() == "true")
            {
                return PartialView("MainControls");
            }
            return RedirectToAction("Index");
        }
        
        [Authorize(Roles = "Player")]
        public IActionResult Action(string selectedAction, string ajax)
        {
            var player = GetPlayer();
            ViewBag.Player = player;
            switch (selectedAction)
            {
                case "overtime":
                    player.WorkOvertime(_rand);
                    if(ajax == "true") return PartialView("DisplayTemplates/view-player");
                    break;
                case "skipturn":
                    player.SkipTurn();
                    if(ajax == "true") return PartialView("DisplayTemplates/view-player");
                    break;
                default:
                    break;
            }
            return RedirectToAction("Index");
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
    }
}
