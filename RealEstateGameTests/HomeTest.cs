using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RealEstateGame.Models;
using Xunit;

namespace RealEstateGameTests
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class HomeTest
    {
        public HomeTest()
        {
        }

        [Fact]
        public void HomeCanGenerate()
        {
            var home = Home.GenerateRandomHome(1, new Random());
            Assert.Equal(1, home.PlayerId);
        }
    }
}
