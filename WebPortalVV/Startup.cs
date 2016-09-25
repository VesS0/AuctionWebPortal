using Microsoft.Owin;
using Owin;
using WebPortalVV.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

[assembly: OwinStartupAttribute(typeof(WebPortalVV.Startup))]
namespace WebPortalVV
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            app.MapSignalR();
            createRolesAndUsers();
        }

        public void createRolesAndUsers()
        {
            ApplicationDbContext context = new ApplicationDbContext();

            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

            if (!roleManager.RoleExists("Admin"))
            {
                var role = new Microsoft.AspNet.Identity.EntityFramework.IdentityRole();
                role.Name = "Admin";
                roleManager.Create(role);

                

                var user = new ApplicationUser();
                user.Name = "Admin";
                user.Surname = "Admin";
                user.UserName = "admin@etf.rs";
                user.Email = "admin@etf.rs";

                string password = "Admin.123";

                var chkUser = userManager.Create(user, password);

                if (chkUser.Succeeded)
                {
                    var result = userManager.AddToRole(user.Id, "Admin");
                }


                //Adding Default Packets
                Packet pckSilv = new Packet(); pckSilv.numberOfTokens = 3; pckSilv.packetName = "SILVER"; pckSilv.packetPrice = 50;
                context.Packets.Add(pckSilv);

                Packet pckGold = new Packet(); pckGold.numberOfTokens = 5; pckGold.packetName = "GOLD"; pckGold.packetPrice = 75;
                context.Packets.Add(pckGold);

                Packet pckPlatinum = new Packet(); pckPlatinum.numberOfTokens = 10; pckPlatinum.packetName = "PLATINUM"; pckPlatinum.packetPrice = 100;
                context.Packets.Add(pckPlatinum);

                context.SaveChanges();
                //Packets Initialized


            }

            if (!roleManager.RoleExists("User"))
            {
                var role = new Microsoft.AspNet.Identity.EntityFramework.IdentityRole();

                role.Name = "User";
                roleManager.Create(role);
            }
        }
    }
}
