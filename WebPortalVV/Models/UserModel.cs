﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System.Data.Entity;
using WebPortalVV.Models;

public partial class UserModel : DbContext
{
    public UserModel()
        : base("name=WebPortalVVEntities")
    {
    }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
    }
}