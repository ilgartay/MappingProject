using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace MapProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteGeometry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<LineString>(
                name: "rota_geom",
                table: "guzergah",
                type: "geometry(LineString,4326)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "rota_mesafe",
                table: "guzergah",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "rota_sure",
                table: "guzergah",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "rota_tarih",
                table: "guzergah",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rota_geom",
                table: "guzergah");

            migrationBuilder.DropColumn(
                name: "rota_mesafe",
                table: "guzergah");

            migrationBuilder.DropColumn(
                name: "rota_sure",
                table: "guzergah");

            migrationBuilder.DropColumn(
                name: "rota_tarih",
                table: "guzergah");
        }
    }
}
