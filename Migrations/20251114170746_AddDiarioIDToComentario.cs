using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booksy_Litlog_Libriscope.Migrations
{
    /// <inheritdoc />
    public partial class AddDiarioIDToComentario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiarioId",
                table: "Comentarios",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comentarios_DiarioId",
                table: "Comentarios",
                column: "DiarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comentarios_Diarios_DiarioId",
                table: "Comentarios",
                column: "DiarioId",
                principalTable: "Diarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comentarios_Diarios_DiarioId",
                table: "Comentarios");

            migrationBuilder.DropIndex(
                name: "IX_Comentarios_DiarioId",
                table: "Comentarios");

            migrationBuilder.DropColumn(
                name: "DiarioId",
                table: "Comentarios");
        }
    }
}
