using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProActive2508.Migrations
{
    /// <inheritdoc />
    public partial class AddUmfrageEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UmfrageKategorie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UmfrageKategorie", x => x.Id);
                    table.CheckConstraint("CK_UmfrageKategorie_Name_NotEmpty", "LEN([Name]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Frage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KategorieId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Frage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Frage_UmfrageKategorie_KategorieId",
                        column: x => x.KategorieId,
                        principalTable: "UmfrageKategorie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Antwort",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FrageId = table.Column<int>(type: "int", nullable: false),
                    ProjektId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Datum = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Antwort", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Antwort_Frage_FrageId",
                        column: x => x.FrageId,
                        principalTable: "Frage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Antwort_Projekte_ProjektId",
                        column: x => x.ProjektId,
                        principalTable: "Projekte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Antwort_FrageId",
                table: "Antwort",
                column: "FrageId");

            migrationBuilder.CreateIndex(
                name: "IX_Antwort_ProjektId",
                table: "Antwort",
                column: "ProjektId");

            migrationBuilder.CreateIndex(
                name: "IX_Frage_KategorieId",
                table: "Frage",
                column: "KategorieId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Antwort");

            migrationBuilder.DropTable(
                name: "Frage");

            migrationBuilder.DropTable(
                name: "UmfrageKategorie");
        }
    }
}
