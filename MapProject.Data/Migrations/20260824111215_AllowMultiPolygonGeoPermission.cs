using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace MapProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultiPolygonGeoPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Geometry>(
                name: "geom",
                table: "tbl_geo_permission",
                type: "geometry(Geometry,4326)",
                nullable: false,
                oldClrType: typeof(Polygon),
                oldType: "geometry(Polygon,4326)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Polygon>(
                name: "geom",
                table: "tbl_geo_permission",
                type: "geometry(Polygon,4326)",
                nullable: false,
                oldClrType: typeof(Geometry),
                oldType: "geometry(Geometry,4326)");
        }
    }
}
