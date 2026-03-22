using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsStore.Migrations
{
    /// <inheritdoc />
    public partial class CleanupVoucherArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys 
    WHERE name = 'FK_Orders_Vouchers_VoucherId' AND parent_object_id = OBJECT_ID('Orders')
)
BEGIN
    ALTER TABLE Orders DROP CONSTRAINT FK_Orders_Vouchers_VoucherId;
END");

            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.ShippingFees', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.ShippingFees;
END");

            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.UserVouchers', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.UserVouchers;
END");

            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.Vouchers', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Vouchers;
END");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Orders_VoucherId' AND object_id = OBJECT_ID('Orders')
)
BEGIN
    DROP INDEX IX_Orders_VoucherId ON Orders;
END");

            migrationBuilder.Sql("IF COL_LENGTH('Orders','DiscountAmount') IS NOT NULL ALTER TABLE Orders DROP COLUMN DiscountAmount;");
            migrationBuilder.Sql("IF COL_LENGTH('Orders','District') IS NOT NULL ALTER TABLE Orders DROP COLUMN District;");
            migrationBuilder.Sql("IF COL_LENGTH('Orders','FinalAmount') IS NOT NULL ALTER TABLE Orders DROP COLUMN FinalAmount;");
            migrationBuilder.Sql("IF COL_LENGTH('Orders','Province') IS NOT NULL ALTER TABLE Orders DROP COLUMN Province;");
            migrationBuilder.Sql("IF COL_LENGTH('Orders','ShippingFee') IS NOT NULL ALTER TABLE Orders DROP COLUMN ShippingFee;");
            migrationBuilder.Sql("IF COL_LENGTH('Orders','VoucherId') IS NOT NULL ALTER TABLE Orders DROP COLUMN VoucherId;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Orders",
                type: "decimal(8,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingFee",
                table: "Orders",
                type: "decimal(8,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "VoucherId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ShippingFees",
                columns: table => new
                {
                    ShippingFeeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DistanceKm = table.Column<int>(type: "int", nullable: true),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Fee = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    Province = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingFees", x => x.ShippingFeeId);
                });

            migrationBuilder.CreateTable(
                name: "Vouchers",
                columns: table => new
                {
                    VoucherId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DailyUsageLimit = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MaxDiscountAmount = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    MinPurchaseAmount = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(8,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vouchers", x => x.VoucherId);
                });

            migrationBuilder.CreateTable(
                name: "UserVouchers",
                columns: table => new
                {
                    UserVoucherId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VoucherId = table.Column<int>(type: "int", nullable: false),
                    LastUsedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedCountToday = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVouchers", x => x.UserVoucherId);
                    table.ForeignKey(
                        name: "FK_UserVouchers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserVouchers_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "VoucherId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_VoucherId",
                table: "Orders",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_UserVouchers_UserId",
                table: "UserVouchers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserVouchers_VoucherId",
                table: "UserVouchers",
                column: "VoucherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Vouchers_VoucherId",
                table: "Orders",
                column: "VoucherId",
                principalTable: "Vouchers",
                principalColumn: "VoucherId");
        }
    }
}
