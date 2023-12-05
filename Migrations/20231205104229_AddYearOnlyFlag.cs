using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RymRss.Migrations
{
    /// <inheritdoc />
    public partial class AddYearOnlyFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "YearOnly",
                table: "Albums",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YearOnly",
                table: "Albums");
        }
    }
}
