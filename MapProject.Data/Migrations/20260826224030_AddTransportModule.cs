using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MapProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransportModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "guzergah",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    renk = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false, defaultValue: "#2563eb"),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guzergah", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "durak",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    guzergah_id = table.Column<int>(type: "integer", nullable: false),
                    sira = table.Column<int>(type: "integer", nullable: false),
                    geom = table.Column<Point>(type: "geometry(Point,4326)", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_durak", x => x.id);
                    table.ForeignKey(
                        name: "FK_durak_guzergah_guzergah_id",
                        column: x => x.guzergah_id,
                        principalTable: "guzergah",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_durak_guzergah_id_sira",
                table: "durak",
                columns: new[] { "guzergah_id", "sira" });

            migrationBuilder.CreateIndex(
                name: "IX_guzergah_ad",
                table: "guzergah",
                column: "ad",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "durak");

            migrationBuilder.DropTable(
                name: "guzergah");
        }
    }
}
