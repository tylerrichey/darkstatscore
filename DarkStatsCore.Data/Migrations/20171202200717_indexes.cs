using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace DarkStatsCore.Data.Migrations
{
    public partial class indexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TrafficStats_Day_In_Out",
                table: "TrafficStats",
                columns: new[] { "Day", "In", "Out" });

            migrationBuilder.CreateIndex(
                name: "IX_TrafficStats_Ip_Day_Hostname_In_LastSeen_Mac_Out",
                table: "TrafficStats",
                columns: new[] { "Ip", "Day", "Hostname", "In", "LastSeen", "Mac", "Out" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrafficStats_Day_In_Out",
                table: "TrafficStats");

            migrationBuilder.DropIndex(
                name: "IX_TrafficStats_Ip_Day_Hostname_In_LastSeen_Mac_Out",
                table: "TrafficStats");
        }
    }
}
