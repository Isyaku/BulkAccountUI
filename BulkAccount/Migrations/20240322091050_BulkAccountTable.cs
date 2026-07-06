using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BulkAccount.Migrations
{
    /// <inheritdoc />
    public partial class BulkAccountTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BulkAccount",
                columns: table => new
                {
                    AccountID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dob = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<int>(type: "int", nullable: false),
                    Bvn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cif = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOpened = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BranchCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IDType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IDNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GlCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MktByID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MktForID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherField = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkAccount", x => x.AccountID);
                });

            migrationBuilder.CreateTable(
                name: "BulkAccountUpload",
                columns: table => new
                {
                    UploadId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkAccountUpload", x => x.UploadId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BulkAccount");

            migrationBuilder.DropTable(
                name: "BulkAccountUpload");
        }
    }
}
