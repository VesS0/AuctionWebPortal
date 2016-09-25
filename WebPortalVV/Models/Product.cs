using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebPortalVV.Models
{
    [Table("Products")]
    public partial class Product
    {
        public Product()
        {
            auctions = new HashSet<Auction>();
        }

        [Key]
        public long idProduct { get; set; }
        public string productName { get; set; }
        public byte[] image { get; set; }

        public virtual ICollection<Auction> auctions { get; set; }
    }
}