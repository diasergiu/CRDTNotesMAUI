using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabaseLibrary.Migrations
{
    /// <inheritdoc />
    public partial class addCRDTCharacter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CRDTCharacter",
                columns: table => new
                {
                    IdCharacter = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdLeftCharacter = table.Column<int>(type: "INTEGER", nullable: true),
                    IdRightCharacter = table.Column<int>(type: "INTEGER", nullable: true),
                    IdNote = table.Column<Guid>(type: "TEXT", nullable: false),
                    Character = table.Column<char>(type: "TEXT", nullable: false),
                    Opperation = table.Column<string>(type: "TEXT", nullable: false),
                    ClockDateTime = table.Column<string>(type: "TEXT", nullable: false),
                    Tombstone = table.Column<bool>(type: "INTEGER", nullable: false),
                    Discriminator = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CRDTCharacter", x => x.IdCharacter);
                    table.ForeignKey(
                        name: "FK_CRDTCharacter_CRDTCharacter_IdLeftCharacter",
                        column: x => x.IdLeftCharacter,
                        principalTable: "CRDTCharacter",
                        principalColumn: "IdCharacter");
                    table.ForeignKey(
                        name: "FK_CRDTCharacter_CRDTCharacter_IdRightCharacter",
                        column: x => x.IdRightCharacter,
                        principalTable: "CRDTCharacter",
                        principalColumn: "IdCharacter");
                    table.ForeignKey(
                        name: "FK_CRDTCharacter_Notes_IdNote",
                        column: x => x.IdNote,
                        principalTable: "Notes",
                        principalColumn: "IdNote",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CRDTCharacter_IdLeftCharacter",
                table: "CRDTCharacter",
                column: "IdLeftCharacter");

            migrationBuilder.CreateIndex(
                name: "IX_CRDTCharacter_IdNote",
                table: "CRDTCharacter",
                column: "IdNote");

            migrationBuilder.CreateIndex(
                name: "IX_CRDTCharacter_IdRightCharacter",
                table: "CRDTCharacter",
                column: "IdRightCharacter");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CRDTCharacter");
        }
    }
}
