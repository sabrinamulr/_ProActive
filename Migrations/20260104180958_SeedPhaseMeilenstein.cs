using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProActive2508.Migrations
{
    /// <inheritdoc />
    public partial class SeedPhaseMeilenstein : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Meilenstein_Benutzer_GenehmigerbenutzerId",
                table: "Meilenstein");

            migrationBuilder.DropForeignKey(
                name: "FK_Meilenstein_ProjektPhase_ProjektphasenId",
                table: "Meilenstein");

            migrationBuilder.DropIndex(
                name: "IX_Meilenstein_GenehmigerbenutzerId",
                table: "Meilenstein");

            migrationBuilder.DropIndex(
                name: "IX_Meilenstein_ProjektphasenId",
                table: "Meilenstein");

            migrationBuilder.DropColumn(
                name: "Erreichtdatum",
                table: "Meilenstein");

            migrationBuilder.DropColumn(
                name: "GenehmigerbenutzerId",
                table: "Meilenstein");

            migrationBuilder.DropColumn(
                name: "ProjektphasenId",
                table: "Meilenstein");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Meilenstein");

            migrationBuilder.DropColumn(
                name: "Zieldatum",
                table: "Meilenstein");

            migrationBuilder.CreateTable(
                name: "PhaseMeilenstein",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjektphasenId = table.Column<int>(type: "int", nullable: false),
                    MeilensteinId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Zieldatum = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Erreichtdatum = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GenehmigerbenutzerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhaseMeilenstein", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhaseMeilenstein_Benutzer_GenehmigerbenutzerId",
                        column: x => x.GenehmigerbenutzerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhaseMeilenstein_Meilenstein_MeilensteinId",
                        column: x => x.MeilensteinId,
                        principalTable: "Meilenstein",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhaseMeilenstein_ProjektPhase_ProjektphasenId",
                        column: x => x.ProjektphasenId,
                        principalTable: "ProjektPhase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Meilenstein",
                columns: new[] { "Id", "Bezeichnung" },
                values: new object[,]
                {
                    { 1, "Quotation" },
                    { 2, "Program Kick-Off" },
                    { 3, "Prototype Design" },
                    { 4, "Production Design" },
                    { 5, "Off Process Tools" },
                    { 6, "Customer PPAP" },
                    { 7, "Production Launch" },
                    { 8, "Productio Transition" },
                    { 9, "End" }
                });

            migrationBuilder.InsertData(
                table: "Phase",
                columns: new[] { "Id", "Bezeichnung", "Kurzbezeichnung" },
                values: new object[,]
                {
                    { 1, "Quotation", "P0" },
                    { 2, "Program Preparation and Kick-Off", "P1" },
                    { 3, "Prototype Design", "P2" },
                    { 4, "Production Design", "P3" },
                    { 5, "Off Process Samples", "P4" },
                    { 6, "Customer PPAP Preparation", "P5" },
                    { 7, "Production Launch", "P6" },
                    { 8, "End of Regular Production & Transition to service", "P7" },
                    { 9, "Product Close-Out", "P8" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhaseMeilenstein_GenehmigerbenutzerId",
                table: "PhaseMeilenstein",
                column: "GenehmigerbenutzerId");

            migrationBuilder.CreateIndex(
                name: "IX_PhaseMeilenstein_MeilensteinId",
                table: "PhaseMeilenstein",
                column: "MeilensteinId");

            migrationBuilder.CreateIndex(
                name: "IX_PhaseMeilenstein_ProjektphasenId",
                table: "PhaseMeilenstein",
                column: "ProjektphasenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhaseMeilenstein");

            migrationBuilder.DeleteData(
                table: "Meilenstein",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Meilenstein",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Meilenstein",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Meilenstein",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Meilenstein",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Meilenstein",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Meilenstein",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Meilenstein",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Meilenstein",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Phase",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Phase",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Phase",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Phase",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Phase",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Phase",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Phase",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Phase",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Phase",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.AddColumn<DateTime>(
                name: "Erreichtdatum",
                table: "Meilenstein",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GenehmigerbenutzerId",
                table: "Meilenstein",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProjektphasenId",
                table: "Meilenstein",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Meilenstein",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Zieldatum",
                table: "Meilenstein",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Meilenstein_GenehmigerbenutzerId",
                table: "Meilenstein",
                column: "GenehmigerbenutzerId");

            migrationBuilder.CreateIndex(
                name: "IX_Meilenstein_ProjektphasenId",
                table: "Meilenstein",
                column: "ProjektphasenId");

            migrationBuilder.AddForeignKey(
                name: "FK_Meilenstein_Benutzer_GenehmigerbenutzerId",
                table: "Meilenstein",
                column: "GenehmigerbenutzerId",
                principalTable: "Benutzer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Meilenstein_ProjektPhase_ProjektphasenId",
                table: "Meilenstein",
                column: "ProjektphasenId",
                principalTable: "ProjektPhase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
