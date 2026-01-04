using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProActive2508.Migrations
{
    /// <inheritdoc />
    public partial class RenameProjektphasenId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjektPhasenMA_ProjektPhase_Projektphasen_id",
                table: "ProjektPhasenMA");

            migrationBuilder.RenameColumn(
                name: "Projektphasen_id",
                table: "ProjektPhasenMA",
                newName: "ProjektphasenId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjektPhasenMA_Projektphasen_id",
                table: "ProjektPhasenMA",
                newName: "IX_ProjektPhasenMA_ProjektphasenId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjektPhasenMA_ProjektPhase_ProjektphasenId",
                table: "ProjektPhasenMA",
                column: "ProjektphasenId",
                principalTable: "ProjektPhase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjektPhasenMA_ProjektPhase_ProjektphasenId",
                table: "ProjektPhasenMA");

            migrationBuilder.RenameColumn(
                name: "ProjektphasenId",
                table: "ProjektPhasenMA",
                newName: "Projektphasen_id");

            migrationBuilder.RenameIndex(
                name: "IX_ProjektPhasenMA_ProjektphasenId",
                table: "ProjektPhasenMA",
                newName: "IX_ProjektPhasenMA_Projektphasen_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjektPhasenMA_ProjektPhase_Projektphasen_id",
                table: "ProjektPhasenMA",
                column: "Projektphasen_id",
                principalTable: "ProjektPhase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
