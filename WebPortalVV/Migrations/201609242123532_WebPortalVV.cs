namespace WebPortalVV.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class WebPortalVV : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Auctions",
                c => new
                    {
                        idAuction = c.Long(nullable: false, identity: true),
                        secondsLasting = c.Long(nullable: false),
                        timeCreated = c.DateTime(nullable: false),
                        timeOpened = c.DateTime(),
                        timeClosed = c.DateTime(),
                        startingPrice = c.Int(nullable: false),
                        totalPriceIncrease = c.Int(nullable: false),
                        bidIncrement = c.Int(nullable: false),
                        status = c.Int(nullable: false),
                        lastBidder = c.String(),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                        product_idProduct = c.Long(),
                    })
                .PrimaryKey(t => t.idAuction)
                .ForeignKey("dbo.Products", t => t.product_idProduct)
                .Index(t => t.product_idProduct);
            
            CreateTable(
                "dbo.Bids",
                c => new
                    {
                        idBid = c.Long(nullable: false, identity: true),
                        timeSent = c.DateTime(nullable: false),
                        auction_idAuction = c.Long(),
                        biddingUser_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.idBid)
                .ForeignKey("dbo.Auctions", t => t.auction_idAuction)
                .ForeignKey("dbo.AspNetUsers", t => t.biddingUser_Id)
                .Index(t => t.auction_idAuction)
                .Index(t => t.biddingUser_Id);
            
            CreateTable(
                "dbo.AspNetUsers",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Name = c.String(),
                        Surname = c.String(),
                        Tokens = c.Int(nullable: false),
                        Email = c.String(maxLength: 256),
                        EmailConfirmed = c.Boolean(nullable: false),
                        PasswordHash = c.String(),
                        SecurityStamp = c.String(),
                        PhoneNumber = c.String(),
                        PhoneNumberConfirmed = c.Boolean(nullable: false),
                        TwoFactorEnabled = c.Boolean(nullable: false),
                        LockoutEndDateUtc = c.DateTime(),
                        LockoutEnabled = c.Boolean(nullable: false),
                        AccessFailedCount = c.Int(nullable: false),
                        UserName = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName, unique: true, name: "UserNameIndex");
            
            CreateTable(
                "dbo.AspNetUserClaims",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        ClaimType = c.String(),
                        ClaimValue = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetUserLogins",
                c => new
                    {
                        LoginProvider = c.String(nullable: false, maxLength: 128),
                        ProviderKey = c.String(nullable: false, maxLength: 128),
                        UserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetUserRoles",
                c => new
                    {
                        UserId = c.String(nullable: false, maxLength: 128),
                        RoleId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetRoles", t => t.RoleId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);
            
            CreateTable(
                "dbo.Products",
                c => new
                    {
                        idProduct = c.Long(nullable: false, identity: true),
                        productName = c.String(),
                        image = c.Binary(),
                    })
                .PrimaryKey(t => t.idProduct);
            
            CreateTable(
                "dbo.Orders",
                c => new
                    {
                        idOrder = c.Long(nullable: false, identity: true),
                        timeCreated = c.DateTime(),
                        status = c.Int(nullable: false),
                        orderingUser_Id = c.String(maxLength: 128),
                        packet_idPacket = c.Long(),
                    })
                .PrimaryKey(t => t.idOrder)
                .ForeignKey("dbo.AspNetUsers", t => t.orderingUser_Id)
                .ForeignKey("dbo.Packets", t => t.packet_idPacket)
                .Index(t => t.orderingUser_Id)
                .Index(t => t.packet_idPacket);
            
            CreateTable(
                "dbo.Packets",
                c => new
                    {
                        idPacket = c.Long(nullable: false, identity: true),
                        packetPrice = c.Double(nullable: false),
                        packetName = c.String(),
                        numberOfTokens = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.idPacket);
            
            CreateTable(
                "dbo.AspNetRoles",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Name = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "RoleNameIndex");
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles");
            DropForeignKey("dbo.Orders", "packet_idPacket", "dbo.Packets");
            DropForeignKey("dbo.Orders", "orderingUser_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Auctions", "product_idProduct", "dbo.Products");
            DropForeignKey("dbo.Bids", "biddingUser_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserClaims", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Bids", "auction_idAuction", "dbo.Auctions");
            DropIndex("dbo.AspNetRoles", "RoleNameIndex");
            DropIndex("dbo.Orders", new[] { "packet_idPacket" });
            DropIndex("dbo.Orders", new[] { "orderingUser_Id" });
            DropIndex("dbo.AspNetUserRoles", new[] { "RoleId" });
            DropIndex("dbo.AspNetUserRoles", new[] { "UserId" });
            DropIndex("dbo.AspNetUserLogins", new[] { "UserId" });
            DropIndex("dbo.AspNetUserClaims", new[] { "UserId" });
            DropIndex("dbo.AspNetUsers", "UserNameIndex");
            DropIndex("dbo.Bids", new[] { "biddingUser_Id" });
            DropIndex("dbo.Bids", new[] { "auction_idAuction" });
            DropIndex("dbo.Auctions", new[] { "product_idProduct" });
            DropTable("dbo.AspNetRoles");
            DropTable("dbo.Packets");
            DropTable("dbo.Orders");
            DropTable("dbo.Products");
            DropTable("dbo.AspNetUserRoles");
            DropTable("dbo.AspNetUserLogins");
            DropTable("dbo.AspNetUserClaims");
            DropTable("dbo.AspNetUsers");
            DropTable("dbo.Bids");
            DropTable("dbo.Auctions");
        }
    }
}
