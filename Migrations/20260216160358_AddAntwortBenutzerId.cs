using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProActive2508.Migrations
{
    /// <inheritdoc />
    public partial class AddAntwortBenutzerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BenutzerId",
                table: "Antwort",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE A SET A.BenutzerId = P.BenutzerId " +
                "FROM [Antwort] AS A " +
                "INNER JOIN [Projekte] AS P ON A.ProjektId = P.Id " +
                "WHERE A.BenutzerId IS NULL OR A.BenutzerId = 0;"
            );

            migrationBuilder.AlterColumn<int>(
                name: "BenutzerId",
                table: "Antwort",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Antwort_BenutzerId",
                table: "Antwort",
                column: "BenutzerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Antwort_Benutzer_BenutzerId",
                table: "Antwort",
                column: "BenutzerId",
                principalTable: "Benutzer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Antwort_Benutzer_BenutzerId",
                table: "Antwort");

            migrationBuilder.DropIndex(
                name: "IX_Antwort_BenutzerId",
                table: "Antwort");

            migrationBuilder.DropColumn(
                name: "BenutzerId",
                table: "Antwort");
        }
    }
}
