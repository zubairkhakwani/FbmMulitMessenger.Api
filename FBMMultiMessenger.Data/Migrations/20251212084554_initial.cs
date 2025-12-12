using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactUs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    IsReplied = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RepliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactUs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PricingTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MinAccounts = table.Column<int>(type: "integer", nullable: false),
                    MaxAccounts = table.Column<int>(type: "integer", nullable: false),
                    PricePerAccount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Extension_Version = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    ContactNumber = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DefaultMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Message = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaultMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DefaultMessages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocalServers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UniqueId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TotalMemoryGB = table.Column<double>(type: "double precision", nullable: false),
                    LogicalProcessors = table.Column<int>(type: "integer", nullable: false),
                    ProcessorName = table.Column<string>(type: "text", nullable: false),
                    CoreCount = table.Column<int>(type: "integer", nullable: false),
                    MaxClockSpeedMHz = table.Column<int>(type: "integer", nullable: false),
                    CurrentClockSpeedMHz = table.Column<int>(type: "integer", nullable: false),
                    CpuArchitecture = table.Column<string>(type: "text", nullable: false),
                    CpuThreadCount = table.Column<int>(type: "integer", nullable: false),
                    GraphicsCards = table.Column<string>(type: "text", nullable: false),
                    TotalStorageGB = table.Column<double>(type: "double precision", nullable: false),
                    HasSSD = table.Column<bool>(type: "boolean", nullable: false),
                    OperatingSystem = table.Column<string>(type: "text", nullable: false),
                    Is64BitOS = table.Column<bool>(type: "boolean", nullable: false),
                    SystemUUID = table.Column<string>(type: "text", nullable: false),
                    MotherboardSerial = table.Column<string>(type: "text", nullable: false),
                    ProcessorId = table.Column<string>(type: "text", nullable: false),
                    MaxBrowserCapacity = table.Column<int>(type: "integer", nullable: false),
                    ActiveBrowserCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsOnline = table.Column<bool>(type: "boolean", nullable: false),
                    IsSuperServer = table.Column<bool>(type: "boolean", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalServers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalServers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Proxies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Ip_Port = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proxies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proxies_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaxLimit = table.Column<int>(type: "integer", nullable: false),
                    LimitUsed = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CanRunOnOurServer = table.Column<bool>(type: "boolean", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VerificationTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Otp = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsEmailVerification = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    DefaultMessageId = table.Column<int>(type: "integer", nullable: true),
                    LocalServerId = table.Column<int>(type: "integer", nullable: true),
                    ProxyId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FbAccountId = table.Column<string>(type: "text", nullable: false),
                    Cookie = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_DefaultMessages_DefaultMessageId",
                        column: x => x.DefaultMessageId,
                        principalTable: "DefaultMessages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Accounts_LocalServers_LocalServerId",
                        column: x => x.LocalServerId,
                        principalTable: "LocalServers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Accounts_Proxies_ProxyId",
                        column: x => x.ProxyId,
                        principalTable: "Proxies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Accounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentVerifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    HandledByUserId = table.Column<int>(type: "integer", nullable: true),
                    SubscriptionId = table.Column<int>(type: "integer", nullable: true),
                    AccountsPurchased = table.Column<int>(type: "integer", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    ActualPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SubmissionNote = table.Column<string>(type: "text", nullable: true),
                    ReviewNote = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RejectionReason = table.Column<int>(type: "integer", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentVerifications_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentVerifications_Users_HandledByUserId",
                        column: x => x.HandledByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentVerifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Chats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    FbUserId = table.Column<string>(type: "text", nullable: true),
                    FbAccountId = table.Column<string>(type: "text", nullable: true),
                    FBChatId = table.Column<string>(type: "text", nullable: true),
                    FbListingId = table.Column<string>(type: "text", nullable: true),
                    FbListingTitle = table.Column<string>(type: "text", nullable: true),
                    FbListingLocation = table.Column<string>(type: "text", nullable: true),
                    FBListingImage = table.Column<string>(type: "text", nullable: true),
                    UserProfileImage = table.Column<string>(type: "text", nullable: true),
                    FbListingPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chats_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Chats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentVerificationImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaymentVerificationId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentVerificationImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentVerificationImages_PaymentVerifications_PaymentVerif~",
                        column: x => x.PaymentVerificationId,
                        principalTable: "PaymentVerifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatId = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    IsReceived = table.Column<bool>(type: "boolean", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    IsSent = table.Column<bool>(type: "boolean", nullable: false),
                    IsTextMessage = table.Column<bool>(type: "boolean", nullable: false),
                    IsImageMessage = table.Column<bool>(type: "boolean", nullable: false),
                    IsVideoMessage = table.Column<bool>(type: "boolean", nullable: false),
                    IsAudioMessage = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "PricingTiers",
                columns: new[] { "Id", "MaxAccounts", "MinAccounts", "PricePerAccount" },
                values: new object[,]
                {
                    { 1, 10, 1, 100m },
                    { 2, 20, 11, 50m },
                    { 3, 100, 21, 40m },
                    { 4, 2147483647, 101, 30m }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Customer" },
                    { 2, new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Admin" },
                    { 3, new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), "SuperAdmin" },
                    { 4, new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), "SuperServer" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ContactNumber", "CreatedAt", "Email", "IsActive", "IsEmailVerified", "Name", "Password", "RoleId" },
                values: new object[,]
                {
                    { 1, "03330337272", new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), "zbrkhakwani@gmail.com", true, false, "Zubair Khakwani", "Zubair!", 3 },
                    { 2, "03330337272", new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), "shaheersk12@gmail.com", true, false, "Shaheer Khawjikzai", "Shaheer1!", 3 },
                    { 3, "03330337272", new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), "test@gmail.com", true, false, "Test_Customer", "Test1!", 1 },
                    { 4, "03330337272", new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), "admin@gmail.com", true, false, "Test_Admin", "Admin1!", 2 },
                    { 5, "03330337272", new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), "super@gmail.com", true, false, "Super_Server", "Super1!", 4 }
                });

            migrationBuilder.InsertData(
                table: "Subscriptions",
                columns: new[] { "Id", "CanRunOnOurServer", "ExpiredAt", "IsActive", "LimitUsed", "MaxLimit", "StartedAt", "UserId" },
                values: new object[,]
                {
                    { 1, false, new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), false, 0, 100, new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 2, false, new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), false, 0, 100, new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), 2 },
                    { 3, false, new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), false, 0, 50, new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), 3 },
                    { 4, false, new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), false, 0, 50, new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), 4 },
                    { 5, false, new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), false, 0, 1000, new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_DefaultMessageId",
                table: "Accounts",
                column: "DefaultMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_LocalServerId",
                table: "Accounts",
                column: "LocalServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ProxyId",
                table: "Accounts",
                column: "ProxyId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId",
                table: "Accounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChatId",
                table: "ChatMessages",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_AccountId",
                table: "Chats",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_UserId",
                table: "Chats",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DefaultMessages_UserId",
                table: "DefaultMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalServers_UserId",
                table: "LocalServers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVerificationImages_PaymentVerificationId",
                table: "PaymentVerificationImages",
                column: "PaymentVerificationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVerifications_HandledByUserId",
                table: "PaymentVerifications",
                column: "HandledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVerifications_SubscriptionId",
                table: "PaymentVerifications",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVerifications_UserId",
                table: "PaymentVerifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Proxies_UserId",
                table: "Proxies",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationTokens_UserId",
                table: "VerificationTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ContactUs");

            migrationBuilder.DropTable(
                name: "PaymentVerificationImages");

            migrationBuilder.DropTable(
                name: "PricingTiers");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "VerificationTokens");

            migrationBuilder.DropTable(
                name: "Chats");

            migrationBuilder.DropTable(
                name: "PaymentVerifications");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "DefaultMessages");

            migrationBuilder.DropTable(
                name: "LocalServers");

            migrationBuilder.DropTable(
                name: "Proxies");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
