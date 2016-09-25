using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace WebPortalVV.Models
{
    public enum AuctionStatus
    {
        DRAFT=0,READY,OPEN,SOLD,EXPIRED,DELETED
    }

    [Table("Auctions")]
    public partial class Auction
    {
        public Auction()
        {
            bids = new HashSet<Bid>();
        }

        [Key]
        public long idAuction { get; set; }

        public long secondsLasting { get; set; }
        public DateTime timeCreated { get; set; }
        public DateTime? timeOpened { get; set; }
        public DateTime? timeClosed { get; set; }

        public int startingPrice { get; set; }
        public int totalPriceIncrease { get; set; }
        public int bidIncrement { get; set; }
        public AuctionStatus status { get; set; }

        public virtual Product product { get; set; }

        public virtual ICollection<Bid> bids { get; set; }


        //public virtual Account lastBidder { get; set; }
        public string lastBidder { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }
    }


}