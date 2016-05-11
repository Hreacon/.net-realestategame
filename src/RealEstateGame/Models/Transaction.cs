using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Entity.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateGame.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }
        [ForeignKey("Player")]
        public int PlayerId { get; set; }
        [ForeignKey("Home")]
        public int HomeId { get; set; }

        public int TurnNum { get; set; }
        public double Amount { get; set; }

        public Transaction(int playerId, int homeId, int turnNum, double amount)
        {
            PlayerId = playerId;
            HomeId = homeId;
            TurnNum = turnNum;
            Amount = amount;
        }

        public Transaction()
        {
        }
    }
}
