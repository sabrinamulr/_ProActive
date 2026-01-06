using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProActive2508.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingColumns0601 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('ProjektPhase', 'VerantwortlicherbenutzerId') IS NULL
BEGIN
    ALTER TABLE [ProjektPhase] ADD [VerantwortlicherbenutzerId] int NOT NULL DEFAULT 0;
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProjektPhase_VerantwortlicherbenutzerId' AND object_id = OBJECT_ID('ProjektPhase'))
BEGIN
    CREATE INDEX [IX_ProjektPhase_VerantwortlicherbenutzerId] ON [ProjektPhase] ([VerantwortlicherbenutzerId]);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ProjektPhase_Benutzer_VerantwortlicherbenutzerId')
BEGIN
    ALTER TABLE [ProjektPhase] ADD CONSTRAINT [FK_ProjektPhase_Benutzer_VerantwortlicherbenutzerId]
        FOREIGN KEY ([VerantwortlicherbenutzerId]) REFERENCES [Benutzer] ([Id]) ON DELETE NO ACTION;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('ProjektPhasenMA', 'Zustandigkeit') IS NULL
BEGIN
    ALTER TABLE [ProjektPhasenMA] ADD [Zustandigkeit] varchar(max) NULL;
END
ELSE
BEGIN
    ALTER TABLE [ProjektPhasenMA] ALTER COLUMN [Zustandigkeit] varchar(max) NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Antwort', 'Datum') IS NULL
BEGIN
    ALTER TABLE [Antwort] ADD [Datum] datetime2 NOT NULL DEFAULT ('0001-01-01T00:00:00');
END
ELSE
BEGIN
    ALTER TABLE [Antwort] ALTER COLUMN [Datum] datetime2 NOT NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ProjektPhase_Benutzer_VerantwortlicherbenutzerId')
BEGIN
    ALTER TABLE [ProjektPhase] DROP CONSTRAINT [FK_ProjektPhase_Benutzer_VerantwortlicherbenutzerId];
END
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProjektPhase_VerantwortlicherbenutzerId' AND object_id = OBJECT_ID('ProjektPhase'))
BEGIN
    DROP INDEX [IX_ProjektPhase_VerantwortlicherbenutzerId] ON [ProjektPhase];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('ProjektPhase', 'VerantwortlicherbenutzerId') IS NOT NULL
BEGIN
    ALTER TABLE [ProjektPhase] DROP COLUMN [VerantwortlicherbenutzerId];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('ProjektPhasenMA', 'Zustandigkeit') IS NOT NULL
BEGIN
    ALTER TABLE [ProjektPhasenMA] DROP COLUMN [Zustandigkeit];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Antwort', 'Datum') IS NOT NULL
BEGIN
    ALTER TABLE [Antwort] DROP COLUMN [Datum];
END
");
        }
    }
}
