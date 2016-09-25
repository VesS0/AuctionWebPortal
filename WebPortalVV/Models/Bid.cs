using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebPortalVV.Models
{
    [Table("Bids")]
    public class Bid
    {
        [Key]
        public long idBid { get; set; }
        public DateTime timeSent { get; set; }

        public virtual Auction auction { get; set; }



        public virtual ApplicationUser biddingUser { get; set; }
        //public virtual Account account { get; set; }

    }
}