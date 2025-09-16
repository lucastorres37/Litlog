using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy_Litlog_Libriscope.Migrations
{
    /// <inheritdoc />
    public partial class RemovingLivroId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LivroId",
                table: "Livros");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LivroId",
                table: "Livros",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
