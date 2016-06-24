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
using Microsoft.Data.Entity;

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
                _context.Renters.AddRange(Renter.GenerateRenters(player.PlayerId));
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
            if (id <= 0) return RedirectToAction("ViewMarket");
            var ajax = Request.Form["ajax"].ToString();
            var player = GetPlayer(); 
            var success = player.BuyHome(id);
            if (success)
            {
                _context.SaveChanges();
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
            return RedirectToAction("ViewMarket");
        }

        [Authorize(Roles = "Player")]
        public IActionResult SellHome(int id)
        {
            var player = GetPlayer();
            player.SellHome(id);
            _context.SaveChanges();
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
            if (!success) return Content("You can't afford to improve this home...");
            _context.SaveChanges();
            ViewBag.Player = player;
            if (ajax == "true")
            {
                return PartialView("PortfolioPartial");
            }
            return RedirectToAction("Portfolio");
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
            _context.SaveChanges();
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
            _context.SaveChanges();
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
            _context.SaveChanges();
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
                    _context.SaveChanges();
                    if(ajax == "true") return PartialView("DisplayTemplates/view-player");
                    break;
                case "skipturn":
                    player.SkipTurn();
                    _context.SaveChanges();
                    if (ajax == "true") return PartialView("DisplayTemplates/view-player");
                    break;
                default:
                    break;
            }
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Player")]
        public IActionResult RentHome(int id, string ajax)
        {
            var player = GetPlayer();
            var home = player.GetHome(id);
            if (home.Condition < 3) return Content("This home is in no shape to rent out");
            if (home.Address == player.Address) return Content("You live here");
            var rent = home.GetRent();
            Random rand = new Random();
            var renters = _context.Renters.Where(m => m.PlayerId == player.PlayerId && m.Renting == 0 && m.Budget > rent).ToList();
            if (renters.Any())
            {
                if (home.Condition < 5)
                {
                    var notRenting = new List<Renter>();
                    foreach (var renter in renters)
                    {
                        if (renter.Damage < 3)
                        {
                            notRenting.Add(renter);
                        }
                    }
                    foreach (var renter in notRenting)
                    {
                        renters.Remove(renter);
                    }
                }
            }
            if (!renters.Any()) return Content("No Renters are interested in this property.");
            ViewBag.Renter = renters[rand.Next(0, renters.Count-1)];
            ViewBag.Home = home;
            ViewBag.Partial = "RenterCard";
            if (ajax == "true") return PartialView(ViewBag.Partial.ToString());
            ViewBag.Player = player;
            return View("MainPage");
        }

        [HttpPost]
        [Authorize(Roles = "Player")]
        public IActionResult AcceptRenter(FormCollection col, string ajax)
        {
            int id = 0;
            int homeId = 0;
            int.TryParse(Request.Form["renterId"], out id);
            int.TryParse(Request.Form["homeId"], out homeId);
            if (id == 0 || homeId == 0) return Content("error");
            var player = GetPlayer();
            var renter = _context.Renters.FirstOrDefault(m => m.RenterId == id);
            var home = player.GetHome(homeId);
            renter.Renting = 1;
            home.Rented = 1;
            home.renter = renter;
            renter.Rent = home.GetRent();
            renter.HomeId = home.HomeId;
            renter.StartTurnNum = player.TurnNum;
            _context.Renters.Update(renter);
            _context.Homes.Update(home);
            _context.Players.Update(player);
            _context.SaveChanges();
            player.CalculateRentalIncome();
            _context.Players.Update(player);
            _context.SaveChanges();
            return RedirectToAction("Portfolio", new {ajax = ajax});
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
        [Authorize(Roles = "Player")]
        public IActionResult RemoveRenter(FormCollection col, string ajax)
        {
            var player = GetPlayer();
            int homeId = 0;
            int.TryParse(Request.Form["homeId"], out homeId);
            if (homeId == 0) return Content("Error");
            var home = player.GetHome(homeId);
            
            var renter = home.renter;
            if (!(player.TurnNum > renter.StartTurnNum + Renter.Term)) return Content("Rental term isn't up yet");

            player.RemoveRenter(home);
            player.CalculateRentalIncome();
            _context.SaveChanges();
            return RedirectToAction("Portfolio", new {ajax = ajax});
        }
    }
}
