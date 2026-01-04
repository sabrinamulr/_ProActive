using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProActive2508.Migrations
{
    /// <inheritdoc />
    public partial class AddProjektPhaseEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Phase",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Bezeichnung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kurzbezeichnung = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Phase", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjektPhase",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjekteId = table.Column<int>(type: "int", nullable: false),
                    PhasenId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Abschlussdatum = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerantwortlicherbenutzerId = table.Column<int>(type: "int", nullable: false),
                    Notizen = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjektPhase", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjektPhase_Benutzer_VerantwortlicherbenutzerId",
                        column: x => x.VerantwortlicherbenutzerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjektPhase_Phase_PhasenId",
                        column: x => x.PhasenId,
                        principalTable: "Phase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjektPhase_Projekte_ProjekteId",
                        column: x => x.ProjekteId,
                        principalTable: "Projekte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Meilenstein",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjektphasenId = table.Column<int>(type: "int", nullable: false),
                    Bezeichnung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Zieldatum = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Erreichtdatum = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GenehmigerbenutzerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meilenstein", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Meilenstein_Benutzer_GenehmigerbenutzerId",
                        column: x => x.GenehmigerbenutzerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Meilenstein_ProjektPhase_ProjektphasenId",
                        column: x => x.ProjektphasenId,
                        principalTable: "ProjektPhase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjektPhasenMA",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BenutzerId = table.Column<int>(type: "int", nullable: false),
                    Projektphasen_id = table.Column<int>(type: "int", nullable: false),
                    Rolle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Zustandigkeit = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjektPhasenMA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjektPhasenMA_Benutzer_BenutzerId",
                        column: x => x.BenutzerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjektPhasenMA_ProjektPhase_Projektphasen_id",
                        column: x => x.Projektphasen_id,
                        principalTable: "ProjektPhase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Meilenstein_GenehmigerbenutzerId",
                table: "Meilenstein",
                column: "GenehmigerbenutzerId");

            migrationBuilder.CreateIndex(
                name: "IX_Meilenstein_ProjektphasenId",
                table: "Meilenstein",
                column: "ProjektphasenId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjektPhase_PhasenId",
                table: "ProjektPhase",
                column: "PhasenId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjektPhase_ProjekteId",
                table: "ProjektPhase",
                column: "ProjekteId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjektPhase_VerantwortlicherbenutzerId",
                table: "ProjektPhase",
                column: "VerantwortlicherbenutzerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjektPhasenMA_BenutzerId",
                table: "ProjektPhasenMA",
                column: "BenutzerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjektPhasenMA_Projektphasen_id",
                table: "ProjektPhasenMA",
                column: "Projektphasen_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Meilenstein");

            migrationBuilder.DropTable(
                name: "ProjektPhasenMA");

            migrationBuilder.DropTable(
                name: "ProjektPhase");

            migrationBuilder.DropTable(
                name: "Phase");
        }
    }
}
