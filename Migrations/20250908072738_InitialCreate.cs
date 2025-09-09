using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProActive2508.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Allergene",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kuerzel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Bezeichnung = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allergene", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Benutzer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Personalnummer = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Stufe = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Abteilung = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Verfuegbarkeit = table.Column<int>(type: "int", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benutzer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Gerichte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Gerichtname = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gerichte", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenueplanTage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tag = table.Column<DateTime>(type: "date", nullable: false),
                    Woche = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenueplanTage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projekte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AufgabeId = table.Column<int>(type: "int", nullable: true),
                    BenutzerId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProjektleiterId = table.Column<int>(type: "int", nullable: false),
                    AuftraggeberId = table.Column<int>(type: "int", nullable: false),
                    Phase = table.Column<int>(type: "int", nullable: false),
                    ProblemId = table.Column<int>(type: "int", nullable: true),
                    Projektbeschreibung = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projekte", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projekte_Benutzer_AuftraggeberId",
                        column: x => x.AuftraggeberId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projekte_Benutzer_BenutzerId",
                        column: x => x.BenutzerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projekte_Benutzer_ProjektleiterId",
                        column: x => x.ProjektleiterId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GerichtAllergene",
                columns: table => new
                {
                    GerichtId = table.Column<int>(type: "int", nullable: false),
                    AllergenId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GerichtAllergene", x => new { x.GerichtId, x.AllergenId });
                    table.ForeignKey(
                        name: "FK_GerichtAllergene_Allergene_AllergenId",
                        column: x => x.AllergenId,
                        principalTable: "Allergene",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GerichtAllergene_Gerichte_GerichtId",
                        column: x => x.GerichtId,
                        principalTable: "Gerichte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Preisverlaeufe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GerichtId = table.Column<int>(type: "int", nullable: false),
                    Preis = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GueltigAb = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preisverlaeufe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Preisverlaeufe_Gerichte_GerichtId",
                        column: x => x.GerichtId,
                        principalTable: "Gerichte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Menueplaene",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenueplanTagId = table.Column<int>(type: "int", nullable: false),
                    PositionNr = table.Column<byte>(type: "tinyint", nullable: false),
                    GerichtId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menueplaene", x => x.Id);
                    table.CheckConstraint("CK_Menueplan_Position", "[PositionNr] IN (1,2)");
                    table.ForeignKey(
                        name: "FK_Menueplaene_Gerichte_GerichtId",
                        column: x => x.GerichtId,
                        principalTable: "Gerichte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Menueplaene_MenueplanTage_MenueplanTagId",
                        column: x => x.MenueplanTagId,
                        principalTable: "MenueplanTage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Aufgaben",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjektId = table.Column<int>(type: "int", nullable: false),
                    BenutzerId = table.Column<int>(type: "int", nullable: false),
                    Aufgabenbeschreibung = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Faellig = table.Column<DateTime>(type: "date", nullable: false),
                    Phase = table.Column<int>(type: "int", nullable: false),
                    Erledigt = table.Column<int>(type: "int", nullable: false),
                    ErstellVon = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aufgaben", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Aufgaben_Benutzer_BenutzerId",
                        column: x => x.BenutzerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Aufgaben_Projekte_ProjektId",
                        column: x => x.ProjektId,
                        principalTable: "Projekte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vorbestellungen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BenutzerId = table.Column<int>(type: "int", nullable: false),
                    MenueplanTagId = table.Column<int>(type: "int", nullable: false),
                    EintragId = table.Column<int>(type: "int", nullable: false),
                    BestelltAm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vorbestellungen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vorbestellungen_Benutzer_BenutzerId",
                        column: x => x.BenutzerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vorbestellungen_Menueplaene_EintragId",
                        column: x => x.EintragId,
                        principalTable: "Menueplaene",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vorbestellungen_MenueplanTage_MenueplanTagId",
                        column: x => x.MenueplanTagId,
                        principalTable: "MenueplanTage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Allergene_Kuerzel",
                table: "Allergene",
                column: "Kuerzel",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Aufgaben_BenutzerId",
                table: "Aufgaben",
                column: "BenutzerId");

            migrationBuilder.CreateIndex(
                name: "IX_Aufgaben_Phase_Faellig",
                table: "Aufgaben",
                columns: new[] { "Phase", "Faellig" });

            migrationBuilder.CreateIndex(
                name: "IX_Aufgaben_ProjektId",
                table: "Aufgaben",
                column: "ProjektId");

            migrationBuilder.CreateIndex(
                name: "IX_Benutzer_Email",
                table: "Benutzer",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Benutzer_Personalnummer",
                table: "Benutzer",
                column: "Personalnummer",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GerichtAllergene_AllergenId",
                table: "GerichtAllergene",
                column: "AllergenId");

            migrationBuilder.CreateIndex(
                name: "IX_Gerichte_Gerichtname",
                table: "Gerichte",
                column: "Gerichtname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Menueplaene_GerichtId",
                table: "Menueplaene",
                column: "GerichtId");

            migrationBuilder.CreateIndex(
                name: "IX_Menueplaene_MenueplanTagId_PositionNr",
                table: "Menueplaene",
                columns: new[] { "MenueplanTagId", "PositionNr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenueplanTage_Tag",
                table: "MenueplanTage",
                column: "Tag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Preisverlaeufe_GerichtId_GueltigAb",
                table: "Preisverlaeufe",
                columns: new[] { "GerichtId", "GueltigAb" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projekte_AuftraggeberId",
                table: "Projekte",
                column: "AuftraggeberId");

            migrationBuilder.CreateIndex(
                name: "IX_Projekte_BenutzerId",
                table: "Projekte",
                column: "BenutzerId");

            migrationBuilder.CreateIndex(
                name: "IX_Projekte_ProjektleiterId",
                table: "Projekte",
                column: "ProjektleiterId");

            migrationBuilder.CreateIndex(
                name: "IX_Projekte_Status_Phase",
                table: "Projekte",
                columns: new[] { "Status", "Phase" });

            migrationBuilder.CreateIndex(
                name: "IX_Vorbestellungen_BenutzerId_EintragId",
                table: "Vorbestellungen",
                columns: new[] { "BenutzerId", "EintragId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vorbestellungen_BenutzerId_MenueplanTagId",
                table: "Vorbestellungen",
                columns: new[] { "BenutzerId", "MenueplanTagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vorbestellungen_EintragId",
                table: "Vorbestellungen",
                column: "EintragId");

            migrationBuilder.CreateIndex(
                name: "IX_Vorbestellungen_MenueplanTagId",
                table: "Vorbestellungen",
                column: "MenueplanTagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Aufgaben");

            migrationBuilder.DropTable(
                name: "GerichtAllergene");

            migrationBuilder.DropTable(
                name: "Preisverlaeufe");

            migrationBuilder.DropTable(
                name: "Vorbestellungen");

            migrationBuilder.DropTable(
                name: "Projekte");

            migrationBuilder.DropTable(
                name: "Allergene");

            migrationBuilder.DropTable(
                name: "Menueplaene");

            migrationBuilder.DropTable(
                name: "Benutzer");

            migrationBuilder.DropTable(
                name: "Gerichte");

            migrationBuilder.DropTable(
                name: "MenueplanTage");
        }
    }
}
