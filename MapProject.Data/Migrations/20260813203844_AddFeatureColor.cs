using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color",
                table: "tbl_polygon",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#009bff");

            migrationBuilder.AddColumn<string>(
                name: "color",
                table: "tbl_point",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#009bff");

            migrationBuilder.AddColumn<string>(
                name: "color",
                table: "tbl_line",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#009bff");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "color",
                table: "tbl_polygon");

            migrationBuilder.DropColumn(
                name: "color",
                table: "tbl_point");

            migrationBuilder.DropColumn(
                name: "color",
                table: "tbl_line");
        }
    }
}
