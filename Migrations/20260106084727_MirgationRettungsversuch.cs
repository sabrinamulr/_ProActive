using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProActive2508.Migrations
{
    /// <inheritdoc />
    public partial class MirgationRettungsversuch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjektPhasenId",
                table: "Aufgaben",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Aufgaben_ProjektPhasenId",
                table: "Aufgaben",
                column: "ProjektPhasenId");

            migrationBuilder.AddForeignKey(
                name: "FK_Aufgaben_ProjektPhase_ProjektPhasenId",
                table: "Aufgaben",
                column: "ProjektPhasenId",
                principalTable: "ProjektPhase",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Aufgaben_ProjektPhase_ProjektPhasenId",
                table: "Aufgaben");

            migrationBuilder.DropIndex(
                name: "IX_Aufgaben_ProjektPhasenId",
                table: "Aufgaben");

            migrationBuilder.DropColumn(
                name: "ProjektPhasenId",
                table: "Aufgaben");
        }
    }
}
