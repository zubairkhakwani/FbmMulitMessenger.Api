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


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasData(
                new User() { Id = 1, Name="Zubair Khakwani", Email = "zbrkhakwani@gmail.com", Password="Zubair!", IsActive = true, CreatedAt = new DateTime(2025, 9, 20) },
                new User() { Id = 2, Name="Shaheer Khawjikzai", Email = "shaheersk12@gmail.com", Password="Shaheer1!", IsActive = true, CreatedAt = new DateTime(2025, 9, 20) }
                );
        }
    }
}
