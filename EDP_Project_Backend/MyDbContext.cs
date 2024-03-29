﻿using EDP_Project_Backend.BackgroundJobs.BackgroundJobsModels;
using EDP_Project_Backend.Models;
using Microsoft.EntityFrameworkCore;
namespace EDP_Project_Backend
{
    public class MyDbContext : DbContext
    {
        private readonly IConfiguration _configuration;
        public MyDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder
        optionsBuilder)
        {
            string? connectionString = _configuration.GetConnectionString(
            "MyConnection");
            if (connectionString != null)
            {
                optionsBuilder.UseMySQL(connectionString);
            }
        }

        // Logging tables for background jobs (please do not touch)
        public DbSet<AllocateVoucherLog> AllocateVoucherLog { get; set; }

        // Make sure to add here when new model is created
        public DbSet<ActivityListing> ActivityListings { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<Tier> Tiers { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Perk> Perks { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Review> Reviews { get; set; }
		public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Notice> Notices { get; set; }
	}
}
