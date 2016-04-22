﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RealEstateGame.Models
{
    public class Home
    {
        [Key]
        public int HomeId  { get; set; }
        [ForeignKey("Player")]
        public int PlayerId   { get;set;}
        // Value of the home
        public int Value    { get;set;}
        // Street Address, 123 Example St
        public string Address { get;set;}
        // 1 if true 0 if not
        public int ForSale  { get;set;}
        public int ForRent  { get;set;}
        public int Rented   { get;set;}
        public int Owned     { get; set; }
        // amount of rent gained per month
        public int Rent     { get;set;}
        // amount asking for sale price
        public int Asking   { get;set;}
        // 0 is broken 10 is fully upgraded
        public int Condition { get;set;}

        public static HashSet<Home> GenerateHomes(int playerId, int amount = 30)
        {
            HashSet<Home> output = new HashSet<Home>();
            Random rand = new Random();
            for (var i = 0; i < amount; i++)
            {
                output.Add(Home.GenerateHome(playerId, rand));
            }
            return output;
        }

        public static Home GenerateHome(int playerId, Random rand)
        {
            int homeNum = rand.Next(1000, 40000);
            string Street = "MX-5 Drive";
            if (homeNum > 35000) Street = Street;
            else if (homeNum > 30000) Street = "Miata Plaza";
            else if (homeNum > 25000) Street = "GT40 Way";
            else if (homeNum > 20000) Street = "Mazda Point";
            else if (homeNum > 15000) Street = "Audi Road";
            else if (homeNum > 10000) Street = "Flat-4 Trail";
            else if (homeNum > 5000) Street = "Volvo Ave";
            else if (homeNum > 1000) Street = "BelAir Thruway";
            string Address = homeNum + " " + Street + " Vancouver WA";
            int Condition = rand.Next(10, 20);
            int Value = 50000 + homeNum*Condition;
            if (homeNum > 30000) Value = Value*2 - Value/2;
            Condition = Condition/2;
            return new Home()
            {
                Address = Address,
                Value = Value,
                PlayerId = playerId,
                ForSale = 1,
                ForRent = 0,
                Rented = 0,
                Rent = 0,
                Asking = Value,
                Condition = Condition,
                Owned = 0
            };
        }
    }
}
