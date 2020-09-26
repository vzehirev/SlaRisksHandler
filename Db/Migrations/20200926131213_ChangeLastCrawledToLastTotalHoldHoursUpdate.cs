using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace XlProcessor.Db.Migrations
{
    public partial class ChangeLastCrawledToLastTotalHoldHoursUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCrawled",
                table: "RiskRecords");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastTotalHoldHoursUpdate",
                table: "RiskRecords",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTotalHoldHoursUpdate",
                table: "RiskRecords");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCrawled",
                table: "RiskRecords",
                type: "datetime2",
                nullable: true);
        }
    }
}
