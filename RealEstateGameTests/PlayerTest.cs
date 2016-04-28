using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RealEstateGame.Models;
using Xunit;

namespace RealEstateGameTests
{
    public class PlayerTest
    {
        [Fact]
        public void PlayerCanGenerate()
        {

            var player = GetPlayer();
            Assert.Equal("test", player.Username);
            Assert.Equal(1000.00, player.Money); // generated in the GeneratePlayer() method
        }

        [Fact]
        public void PlayerCanUseActions()
        {
            var player = GetPlayer();
            Assert.Equal(2,player.Actions); // player starts with two actions
            player.UseAction();
            Assert.Equal(1,player.Actions); // player used an action now has 1
        }

        [Fact]
        public void PlayerChangesTurns()
        {
            var player = GetPlayer();
            Assert.Equal(0, player.TurnNum);
            player.Actions = 0;
            player.NextTurn();
            Assert.Equal(1, player.TurnNum);
            player.UseAction();
            player.UseAction();
            Assert.Equal(2, player.TurnNum);
        }

        public Player GetPlayer()
        {
            var username = "test";
            var id = "thisisanid";
            Player player = Player.GeneratePlayer(new ApplicationUser()
            {
                UserName = username,
                Id = id
            });
            return player;
        }
    }
}
