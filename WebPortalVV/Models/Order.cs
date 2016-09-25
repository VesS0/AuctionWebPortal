using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebPortalVV.Models
{
    public enum OrderStatus
    {
        PENDING,CANCELED,APPROVED
    }

    [Table("Orders")]
    public class Order
    {
        [Key]
        public long idOrder { get; set; }
        public DateTime? timeCreated { get; set; }
        public OrderStatus status { get; set; }

        public virtual ApplicationUser orderingUser { get; set; }
        public virtual Packet packet { get; set; }
    }
}