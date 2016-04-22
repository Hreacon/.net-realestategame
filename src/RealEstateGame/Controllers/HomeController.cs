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
            if (User.IsSignedIn())
            {
                ApplicationUser u = await _userManager.FindByIdAsync(User.GetUserId());
                return u;
            }
            else return null;
        }

        [NonAction]
        private async void SaveUser(ApplicationUser user)
        {
            await _userManager.UpdateAsync(user);
        }

        // Routes

        public IActionResult Index()
        {
            if (User.IsSignedIn())
                return View("MainPage", GetUser().Result);
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
            var user = GetUser().Result;
            switch (selectedAction)
            {
                case "overtime":
                    user.WorkOvertime(_rand);
                    break;
                default:
                    break;
            }
            SaveUser(user);
            return RedirectToAction("Index");
        }

        
    }
}
