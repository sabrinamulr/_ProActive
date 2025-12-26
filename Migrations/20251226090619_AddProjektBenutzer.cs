using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProActive2508.Migrations
{
    /// <inheritdoc />
    public partial class AddProjektBenutzer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjektBenutzer",
                columns: table => new
                {
                    ProjektId = table.Column<int>(type: "int", nullable: false),
                    BenutzerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjektBenutzer", x => new { x.ProjektId, x.BenutzerId });
                    table.ForeignKey(
                        name: "FK_ProjektBenutzer_Benutzer_BenutzerId",
                        column: x => x.BenutzerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjektBenutzer_Projekte_ProjektId",
                        column: x => x.ProjektId,
                        principalTable: "Projekte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjektBenutzer_BenutzerId",
                table: "ProjektBenutzer",
                column: "BenutzerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjektBenutzer");
        }
    }
}
