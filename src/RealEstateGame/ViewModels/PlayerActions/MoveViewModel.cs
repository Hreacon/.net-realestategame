using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateGame.ViewModels.PlayerActions
{
    public class MoveViewModel
    {
        [Required]
        public int HomeId { get; set; }

        [Required]
        public string Desctription { get; set; }
    }
}
