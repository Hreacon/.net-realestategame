using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc;
using RealEstateGame.Models;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace RealEstateGame.Controllers
{
    [Authorize(Roles = "Player")]
    public class LoanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoanController(ApplicationDbContext context)
        {
            _context = context;
        }

        [NonAction]
        public Player GetPlayer()
        {
            if (User.IsSignedIn())
            {
                var userid = User.GetUserId();
                var player = _context.Players.FirstOrDefault(m => m.UserId == userid);
                player.context = _context;
                return player;
            }
            return null;
        }

        [NonAction]
        public double GetAPR()
        {
            return .035;
        }

        [NonAction]
        public double GetFHAAPR()
        {
            return GetAPR() + .03;
        }

        // GET: /<controller>/
        public IActionResult Index(string ajax)
        {
            ViewBag.Player = GetPlayer();
            ViewData["partial"] = "Index";
            if (ajax == "true")
            {
                return PartialView(ViewData["partial"]);
            }
            return View("MainPage");
        }

        public IActionResult Apply(string ajax)
        {
            var player = GetPlayer();
            // Players income is listed income + rentals - expenses, namely mortgages. In the future mortgage payments also come out.
            // TODO calculate total mortgage payments and deduct from income
            // TODO calculate collected rent and add to income
            var income = player.NetPerTurn;
            // current interest rate.. held ??? currently static @ 4.5%
            var currentAPR = GetAPR();
            var principal = Loan.CalculateAffordableAmount(income, currentAPR, 360);
            ViewData["principal"] = principal;
            ViewData["partial"] = "Apply";
            ViewBag.Homes = _context.Homes.Where(m => m.PlayerId == player.PlayerId && m.Owned == 0 && m.ForSale == 1 && m.Asking < principal).OrderBy(m => m.Asking);
            if (ajax == "true")
            {
                return PartialView(ViewData["partial"]);
            }
            ViewBag.Player = player;
            return View("MainPage");
        }
        
        public IActionResult Application(int id, string ajax)
        {
            var player = GetPlayer();
            ViewBag.Player = player;
            ViewData["partial"] = "LoanApplication";
            ViewBag.Home = _context.Homes.FirstOrDefault(m => m.HomeId == id);
            ViewData["apr"] = GetAPR();
            if (ajax == "true")
            {
                return PartialView(ViewData["partial"].ToString());
            }
            return View("MainPage");
        }

        [HttpPost]
        public IActionResult RegularLoanApplication(FormCollection col, string ajax)
        {
            var player = GetPlayer();
            var stringhomeId = Request.Form["homeId"];
            int homeId = 0;
            Int32.TryParse(stringhomeId, out homeId);
            if (homeId > 0)
            {
                var home = _context.Homes.FirstOrDefault(m => m.HomeId == homeId);
                if (player.Money > home.GetDownPayment())
                {
                    // player can afford loan
                    player.Money = player.Money - home.GetDownPayment();
                    home.Owned = 1;
                    var loan = _context.Loans.Add(new Loan(player.PlayerId, home.Asking - home.GetDownPayment(), GetAPR(), 360,
                        player.TurnNum, home)).Entity;
                    home.loan = loan;
                    player.SavePlayerAndHome(home);
                    return RedirectToAction("Index", "Home", new {ajax=ajax});
                }
            }
            // something went wrong
            return RedirectToAction("Application", new {ajax=ajax});
        }

        [HttpPost]
        public IActionResult FhaLoanApplication(FormCollection col, string ajax)
        {
            var player = GetPlayer();
            bool hasFHA = false;
            var loans = player.GetLoans();
            if (loans != null)
            {
                foreach (var loan in loans)
                {
                    if (loan.LoanType == 1)
                    {
                        hasFHA = true;
                        break;
                    }
                }
            }
            if (hasFHA) return Content("You already have an FHA Loan");
            int homeId = 0;
            int.TryParse(Request.Form["homeId"], out homeId);
            if (homeId > 0)
            {
                var home = _context.Homes.FirstOrDefault(m => m.HomeId == homeId);
                var totalCost = home.GetFHADownPayment();
                if (home.Condition < Loan.FHACondition) totalCost += home.CostToCondition(Loan.FHACondition);
                if (player.Money > totalCost)
                {
                    player.Money = player.Money - totalCost;
                    if(home.Condition < Loan.FHACondition) home.ImproveToCondition(Loan.FHACondition);
                    home.Owned = 1;
                    player.MoveIntoHome(home);
                    var loan = _context.Loans.Add(new Loan(player.PlayerId, home.Asking - home.GetFHADownPayment(), GetAPR(), 360,
                        player.TurnNum, home, 1)).Entity;
                    home.loan = loan;
                    player.SavePlayerAndHome(home);
                    return RedirectToAction("Index", "Home", new {ajax=ajax});
                }
            }
            return RedirectToAction("Application", new {ajax = ajax});
        }

        [HttpPost]
        public IActionResult ExtraPayments(FormCollection col, string ajax)
        {
            var player = GetPlayer();
            var loans = player.GetLoans();
            if (loans != null)
            {
                foreach (var loan in loans)
                {
                    var extrapayment = double.Parse(Request.Form[loan.LoanId.ToString()]); 
                    if (!(player.Money+0.01 > extrapayment) || !(extrapayment > 0)) continue; // continue jumps to next iteration of loop
                    if (extrapayment > loan.Principal)
                    {
                        extrapayment = loan.Principal;
                    }
                    loan.MakeExtraPayment(extrapayment);
                    if (loan.Principal <= 0)
                    {
                        var home = _context.Homes.FirstOrDefault(m => m.HomeId == loan.HomeId);
                        home.loan = null;
                        _context.Homes.Update(home);
                        _context.Loans.Remove(loan);
                    } else _context.Loans.Update(loan);
                    player.Money = player.Money - extrapayment;
                }
                player.context = _context;
                player.Save();
            }
            return RedirectToAction("Index", new {ajax=ajax});
        }
    }
}
