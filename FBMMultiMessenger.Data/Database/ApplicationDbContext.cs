using FBMMultiMessenger.Data.Database.DbModels;
using Microsoft.EntityFrameworkCore;
using System.Data;

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
        public DbSet<DefaultMessage> DefaultMessages { get; set; }
        public DbSet<VerificationToken> VerificationTokens { get; set; }
        public DbSet<PricingTier> PricingTiers { get; set; }
        public DbSet<PaymentVerification> PaymentVerifications { get; set; }
        public DbSet<PaymentVerificationImage> PaymentVerificationImages { get; set; }
        public DbSet<ContactUs> ContactUs { get; set; }

        public DbSet<Role> Roles { get; set; }
        public DbSet<Settings> Settings { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>().HasData(
               new Role() { Id = 1, Name="Customer", CreatedAt = DateTime.SpecifyKind(new DateTime(2025, 9, 20), DateTimeKind.Utc) },
               new Role() { Id = 2, Name="Admin", CreatedAt = DateTime.SpecifyKind(new DateTime(2025, 9, 20), DateTimeKind.Utc) },
               new Role() { Id = 3, Name="SuperAdmin", CreatedAt = DateTime.SpecifyKind(new DateTime(2025, 9, 20), DateTimeKind.Utc) }
               );

            modelBuilder.Entity<User>().HasData(
                new User() { Id = 1, Name="Zubair Khakwani", Email = "zbrkhakwani@gmail.com", Password="Zubair!", ContactNumber="03330337272", IsActive = true, CreatedAt = DateTime.SpecifyKind(new DateTime(2025, 9, 20), DateTimeKind.Utc), RoleId = 3 },
                new User() { Id = 2, Name="Shaheer Khawjikzai", Email = "shaheersk12@gmail.com", Password="Shaheer1!", ContactNumber="03330337272", IsActive = true, CreatedAt = DateTime.SpecifyKind(new DateTime(2025, 9, 20), DateTimeKind.Utc), RoleId = 3 },
                new User() { Id = 3, Name="Test_Customer", Email = "test@gmail.com", Password="Test1!", ContactNumber="03330337272", IsActive = true, CreatedAt = DateTime.SpecifyKind(new DateTime(2025, 9, 20), DateTimeKind.Utc), RoleId = 1 },
                new User() { Id = 4, Name="Test_Admin", Email = "admin@gmail.com", Password="Admin1!", ContactNumber="03330337272", IsActive = true, CreatedAt = DateTime.SpecifyKind(new DateTime(2025, 9, 20), DateTimeKind.Utc), RoleId = 2 }


                );

            modelBuilder.Entity<Subscription>().HasData(

                new Subscription() { Id =1, UserId = 1, MaxLimit = 100, LimitUsed=0, StartedAt =DateTime.SpecifyKind(new DateTime(2025, 9, 20), DateTimeKind.Utc), ExpiredAt = DateTime.SpecifyKind(new DateTime(2025, 12, 31), DateTimeKind.Utc) },
                new Subscription() { Id =2, UserId = 2, MaxLimit = 100, LimitUsed = 0, StartedAt = DateTime.SpecifyKind(new DateTime(2025, 9, 20), DateTimeKind.Utc), ExpiredAt = DateTime.SpecifyKind(new DateTime(2025, 12, 31), DateTimeKind.Utc) }
                );

            modelBuilder.Entity<PricingTier>().HasData(
                new PricingTier() { Id =1, MinAccounts = 1, MaxAccounts = 10, PricePerAccount = 100 },
                new PricingTier() { Id =2, MinAccounts = 11, MaxAccounts = 20, PricePerAccount = 50 },
                new PricingTier() { Id =3, MinAccounts = 21, MaxAccounts = 100, PricePerAccount = 40 },
                new PricingTier() { Id =4, MinAccounts = 101, MaxAccounts = int.MaxValue, PricePerAccount = 30 }
                );
        }
    }
}
