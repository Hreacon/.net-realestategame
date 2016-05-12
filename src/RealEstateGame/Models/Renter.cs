using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Entity.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateGame.Models
{
    public class Renter
    {
        [Key]
        public int RenterId { get; set; }
        [ForeignKey("Player")]
        public int PlayerId { get; set; }
        public string Name { get; set; }
        public double Budget { get; set; }
        public int Damage { get; set; }
        public int Renting { get; set; }
        public int HomeId { get; set; }

        public static IEnumerable<Renter> GenerateRenters(int playerId, int amount = 30, Random rand = null)
        {
            if(rand == null) rand = new Random();
            List<Renter> output = new List<Renter>();
            for(var i = 0;i<amount;i++)
            {
                output.Add(GenerateRenter(playerId, rand));
            }
            return output;
        }

        private static Renter GenerateRenter(int playerId, Random rand = null)
        {
            if(rand == null) rand = new Random();
            return new Renter()
            {
                Budget = rand.Next(500,3000),
                Damage = rand.Next(0,10),
                HomeId = 0,
                Name = GenerateName(rand),
                PlayerId = playerId,
                Renting = 0
            };
        }

        private static string GenerateName(Random rand = null)
        {
            if(rand == null) rand = new Random();

            List<string> firstNames = new List<string>();
            firstNames.Add("Jay");
            firstNames.Add("Andy");
            firstNames.Add("Chris");
            firstNames.Add("Valerie");
            firstNames.Add("David");
            firstNames.Add("Neal");
            firstNames.Add("Lawton");
            firstNames.Add("Jackson");
            firstNames.Add("Will");
            firstNames.Add("Nick");
            firstNames.Add("Michael");
            firstNames.Add("Michael");
            firstNames.Add("Taylor");
            firstNames.Add("Nathan");
            firstNames.Add("Alex");
            firstNames.Add("Simon");
            firstNames.Add("Sean");
            firstNames.Add("Mary");
            firstNames.Add("Marika");
            firstNames.Add("Molly");
            firstNames.Add("Tim");
            firstNames.Add("Tal");
            firstNames.Add("Jordan");
            firstNames.Add("Joshua");
            firstNames.Add("Austin");
            
            List<string> lastNames = new List<string>();
            lastNames.Add("Meyer");
            lastNames.Add("Warrington");
            lastNames.Add("Kuiper");
            lastNames.Add("Whang");
            lastNames.Add("Kuiper");
            lastNames.Add("Cafazzo");
            lastNames.Add("Meyer");
            lastNames.Add("Temple");
            lastNames.Add("Peerenboom");
            lastNames.Add("Jensen");
            lastNames.Add("Kinsey");
            lastNames.Add("Dada");
            lastNames.Add("Fallenstedt");
            lastNames.Add("Gustafson");
            lastNames.Add("Johnson");

            return firstNames[rand.Next(0, firstNames.Count)] + " " + lastNames[rand.Next(0, lastNames.Count)];
        }
    }
}
