using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Entity.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateGame.Models
{
    public class Home
    {
        public int HomeId  { get; set; }
        [ForeignKey("ApplicationUser")]
        public string UserId   { get;set;}
        public int Value    { get;set;}
        public string Address { get;set;}
        public int ForSale  { get;set;}
        public int ForRent  { get;set;}
        public int Rented   { get;set;}
        public int Rent     { get;set;}
        public int Asking   { get;set;}
        public int Condition { get;set;}
        public int Owned     { get; set; }
    }
}
