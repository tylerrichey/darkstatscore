using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace DarkStatsCore.Data.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrafficStats",
                columns: table => new
                {
                    Ip = table.Column<string>(type: "TEXT", nullable: false),
                    Day = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Hostname = table.Column<string>(type: "TEXT", nullable: true),
                    In = table.Column<long>(type: "INTEGER", nullable: false),
                    LastSeen = table.Column<string>(type: "TEXT", nullable: true),
                    Mac = table.Column<string>(type: "TEXT", nullable: true),
                    Out = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficStats", x => new { x.Ip, x.Day });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrafficStats");
        }
    }
}
