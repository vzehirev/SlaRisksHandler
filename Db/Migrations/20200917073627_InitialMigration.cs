﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace XlProcessor.Db.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RiskRecords",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VLookupName = table.Column<string>(nullable: false),
                    DxcStatus = table.Column<string>(nullable: true),
                    LastStatusChange = table.Column<DateTime>(nullable: true),
                    TotalHoldHours = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatusChanges",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiskRecordId = table.Column<int>(nullable: false),
                    OldStatus = table.Column<string>(nullable: true),
                    NewStatus = table.Column<string>(nullable: false),
                    ChangedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatusChanges_RiskRecords_RiskRecordId",
                        column: x => x.RiskRecordId,
                        principalTable: "RiskRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StatusChanges_RiskRecordId",
                table: "StatusChanges",
                column: "RiskRecordId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StatusChanges");

            migrationBuilder.DropTable(
                name: "RiskRecords");
        }
    }
}
