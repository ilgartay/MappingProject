using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureTrackingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "tbl_polygon",
                newName: "inserted_date");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "tbl_point",
                newName: "inserted_date");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "tbl_line",
                newName: "inserted_date");

            migrationBuilder.AddColumn<int>(
                name: "inserted_user_id",
                table: "tbl_polygon",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "tbl_polygon",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "tbl_polygon",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_date",
                table: "tbl_polygon",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "inserted_user_id",
                table: "tbl_point",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "tbl_point",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "tbl_point",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_date",
                table: "tbl_point",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "inserted_user_id",
                table: "tbl_line",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "tbl_line",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "tbl_line",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_date",
                table: "tbl_line",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_polygon_inserted_user_id",
                table: "tbl_polygon",
                column: "inserted_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_point_inserted_user_id",
                table: "tbl_point",
                column: "inserted_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_line_inserted_user_id",
                table: "tbl_line",
                column: "inserted_user_id");

            // Bu migration'dan önce oluşturulmuş çizimlerin sahibi yok.
            // Sahipsiz kalırlarsa hiçbir kullanıcıya görünmezler; mevcut
            // kayıtları ilk kullanıcıya (admin) devrediyoruz.
            migrationBuilder.Sql(
                "UPDATE tbl_point SET inserted_user_id = 1 WHERE inserted_user_id = 0;");
            migrationBuilder.Sql(
                "UPDATE tbl_line SET inserted_user_id = 1 WHERE inserted_user_id = 0;");
            migrationBuilder.Sql(
                "UPDATE tbl_polygon SET inserted_user_id = 1 WHERE inserted_user_id = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_polygon_inserted_user_id",
                table: "tbl_polygon");

            migrationBuilder.DropIndex(
                name: "IX_tbl_point_inserted_user_id",
                table: "tbl_point");

            migrationBuilder.DropIndex(
                name: "IX_tbl_line_inserted_user_id",
                table: "tbl_line");

            migrationBuilder.DropColumn(
                name: "inserted_user_id",
                table: "tbl_polygon");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "tbl_polygon");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "tbl_polygon");

            migrationBuilder.DropColumn(
                name: "modified_date",
                table: "tbl_polygon");

            migrationBuilder.DropColumn(
                name: "inserted_user_id",
                table: "tbl_point");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "tbl_point");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "tbl_point");

            migrationBuilder.DropColumn(
                name: "modified_date",
                table: "tbl_point");

            migrationBuilder.DropColumn(
                name: "inserted_user_id",
                table: "tbl_line");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "tbl_line");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "tbl_line");

            migrationBuilder.DropColumn(
                name: "modified_date",
                table: "tbl_line");

            migrationBuilder.RenameColumn(
                name: "inserted_date",
                table: "tbl_polygon",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "inserted_date",
                table: "tbl_point",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "inserted_date",
                table: "tbl_line",
                newName: "created_date");
        }
    }
}
