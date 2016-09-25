using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebPortalVV.Models
{
    //public enum PacketName
    //{
    //    SILVER=0,GOLD,PLATINUM
    //}

    [Table("Packets")]
    public class Packet
    {
        public Packet()
        {
            this.orders = new HashSet<Order>();
        }
        [Key]
        public long idPacket { get; set; }
        public double packetPrice { get; set; }
               //PacketName
        public string packetName { get; set; }
        public int numberOfTokens { get; set; }

        public virtual ICollection<Order> orders { get; set; }
    }
}