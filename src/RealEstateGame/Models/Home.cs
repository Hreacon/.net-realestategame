using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateGame.Models
{
    public class Home
    {
        public int HomeId  { get; set; }
        public int Value    { get;set;}
        public string Addre { get;set;}
        public int ForSale  { get;set;}
        public int ForRent  { get;set;}
        public int Rented   { get;set;}
        public int Rent     { get;set;}
        public int Asking   { get;set;}
        public int UserId   { get;set;}
        public int Conditio { get;set;}
    }
}
