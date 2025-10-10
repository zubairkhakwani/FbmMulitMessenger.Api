using FBMMultiMessenger.Data.Database.DbModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Data.DB
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatMessages> ChatMessages { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasData(
                new User() { Id = 1, Name="Zubair Khakwani", Email = "zbrkhakwani@gmail.com", Password="Zubair!", ContactNumber="03330337272", IsActive = true, CreatedAt = new DateTime(2025, 9, 20) },
                new User() { Id = 2, Name="Shaheer Khawjikzai", Email = "shaheersk12@gmail.com", Password="Shaheer1!", ContactNumber="03330337272", IsActive = true, CreatedAt = new DateTime(2025, 9, 20) }
                );

            modelBuilder.Entity<Subscription>().HasData(

                new Subscription() { Id =1, UserId = 1, MaxLimit = 5, LimitUsed=0, StartedAt =new DateTime(2025, 9, 20), ExpiredAt =new DateTime(2025, 10, 20) },
                new Subscription() { Id =2, UserId = 2, MaxLimit = 5, LimitUsed = 0, StartedAt = new DateTime(2025, 9, 20), ExpiredAt = new DateTime(2025, 10, 20) }
                );
        }
    }
}
