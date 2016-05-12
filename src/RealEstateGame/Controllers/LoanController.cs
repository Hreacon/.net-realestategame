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
        private ApplicationDbContext _context;

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
            var income = player.Income;
            // current interest rate.. held ??? currently static @ 4.5%
            var currentAPR = .045;
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
                return PartialView(ViewData["partial"]);
            }
            return View("MainPage");
        }

        [HttpPost]
        public IActionResult RegularLoanApplication(FormCollection col, string ajax)
        {
            // todo ajax
            var player = GetPlayer();
            var stringhomeId = Request.Form["homeId"];
            int homeId = 0;
            Int32.TryParse(stringhomeId, out homeId);
            if (homeId > 0)
            {
                var home = _context.Homes.FirstOrDefault(m => m.HomeId == homeId);
                if (player.Money > home.Asking*.2)
                {
                    // player can afford loan
                    player.Money = player.Money - home.Asking*.2;
                    home.Owned = 1;
                    _context.Loans.Add(new Loan(player.PlayerId, home.Asking - (home.Asking*.2), GetAPR(), 360,
                        player.TurnNum));
                    player.SavePlayerAndHome(home);
                    return RedirectToAction("Index", "Home");
                }
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult ExtraPayments(FormCollection col, string ajax)
        {
            // todo ajax
            var player = GetPlayer();
            var loans = player.GetLoans();
            foreach (var loan in loans)
            {
                var extrapayment = Double.Parse(Request.Form[loan.LoanId.ToString()]);
                if (player.Money > extrapayment && extrapayment > 0)
                {
                    if (extrapayment > loan.Principal)
                    {
                        extrapayment = loan.Principal;
                    }
                    loan.MakeExtraPayment(extrapayment);
                    if (loan.Principal <= 0)
                    {
                        _context.Loans.Remove(loan);
                    }
                    player.Money = player.Money - extrapayment;
                    _context.Loans.Update(loan);
                }
            }
            player.Save();
            return RedirectToAction("Index");
        }
    }
}
