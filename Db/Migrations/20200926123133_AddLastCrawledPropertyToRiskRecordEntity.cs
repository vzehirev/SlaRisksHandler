using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace XlProcessor.Db.Migrations
{
    public partial class AddLastCrawledPropertyToRiskRecordEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastCrawled",
                table: "RiskRecords",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCrawled",
                table: "RiskRecords");
        }
    }
}
