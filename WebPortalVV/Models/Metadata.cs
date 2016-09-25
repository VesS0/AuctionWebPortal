using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebPortalVV.Models
{
    public class AuctionMetadata
    {
        public long idAuction { get; set; }
        public long secondsLasting { get; set; }

        [Display(Name = "Starting price")]
        public int startingPrice { get; set; }

        [Display(Name = "Creation date")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy H:mm:s}", ApplyFormatInEditMode = true)]
        public DateTime? timeCreated { get; set; }

        [Display(Name = "Opening date")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy H:mm:s}", ApplyFormatInEditMode = true)]
        public DateTime? timeOpened { get; set; }

        [Display(Name = "Closing date")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy H:mm:s}", ApplyFormatInEditMode = true)]
        public DateTime? timeClosed { get; set; }

        public AuctionStatus status { get; set; }

        //[display(name = "product name")]
        //public string productname { get; set; }

        public int bidIncrement { get; set; }
    }

    public class ProductMetadata
    {
        public long idProduct { get; set; }
        [Display(Name = "Product name")]
        public string productName { get; set; }
        public byte[] image { get; set; }
    }

    [MetadataType(typeof(AuctionMetadata))]
    public partial class Auction
    {

    }

    [MetadataType(typeof(ProductMetadata))]
    public partial class Product
    {
        [NotMapped]
        public HttpPostedFileBase ImageToUpload { get; set; }

    }

    public partial class AuctionCreate
    {
        public long idAuction { get; set; }

        [Required]
        [Display(Name = "Seconds Lasting")]
        public long secondsLasting { get; set; }

        [Required]
        [Display(Name = "Starting price")]
        public int startingPrice { get; set; }

        [Display(Name = "Creation date")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy H:mm:s}", ApplyFormatInEditMode = true)]
        public DateTime timeCreated { get; set; }

        [Display(Name = "Opening date")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy H:mm:s}", ApplyFormatInEditMode = true)]
        public DateTime? timeOpened { get; set; }

        [Display(Name = "Closing date")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy H:mm:s}", ApplyFormatInEditMode = true)]
        public DateTime? timeClosed { get; set; }

        public AuctionStatus status { get; set; }

        [Display(Name = "Product")]
        public long idProduct { get; set; }

        [Display(Name = "Price Increment(Bid)")]
        public int bidIncrement { get; set; }

    }

    public partial class ProductCreate
    {
        public int idProduct { get; set; }

        [Required]
        [Display(Name = "Product name")]
        [DataType(DataType.Text)]
        public string productName { get; set; }

        [Display(Name = "image")]
        public byte[] image { get; set; }

        [Required]
        [Display(Name = "image")]
        public HttpPostedFileBase ImageToUpload { get; set; }
    }


    public class ChangePriceViewModel
    {
        [Required]
        [Display(Name = "Starting Price")]
        public int startingPrice { get; set; }

        public long idAuction { get; set; }
    }

}