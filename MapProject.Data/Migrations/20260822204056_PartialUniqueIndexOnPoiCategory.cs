using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class PartialUniqueIndexOnPoiCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_poi_category_parent_id_name",
                table: "poi_category");

            migrationBuilder.CreateIndex(
                name: "IX_poi_category_parent_id_name",
                table: "poi_category",
                columns: new[] { "parent_id", "name" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_poi_category_parent_id_name",
                table: "poi_category");

            migrationBuilder.CreateIndex(
                name: "IX_poi_category_parent_id_name",
                table: "poi_category",
                columns: new[] { "parent_id", "name" },
                unique: true);
        }
    }
}
