using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabaseLibrary.Migrations.DbContextServerMigrations
{
    /// <inheritdoc />
    public partial class addCRDTCharacterCompositeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    IdDevice = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.IdDevice);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    IdNote = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreationDate = table.Column<string>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    LastUpdate = table.Column<string>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    DirtyFlagChangesMade = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.IdNote);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    IdUser = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.IdUser);
                });

            migrationBuilder.CreateTable(
                name: "CRDTCharacters",
                columns: table => new
                {
                    IdCharacter = table.Column<int>(type: "INTEGER", nullable: false),
                    IdNote = table.Column<Guid>(type: "TEXT", nullable: false),
                    IdLeftCharacter = table.Column<int>(type: "INTEGER", nullable: true),
                    IdRightCharacter = table.Column<int>(type: "INTEGER", nullable: true),
                    Character = table.Column<char>(type: "TEXT", nullable: false),
                    Opperation = table.Column<string>(type: "TEXT", nullable: false),
                    ClockDateTime = table.Column<string>(type: "TEXT", nullable: false),
                    Tombstone = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CRDTCharacters", x => new { x.IdCharacter, x.IdNote });
                    table.ForeignKey(
                        name: "FK_CRDTCharacters_Notes_IdNote",
                        column: x => x.IdNote,
                        principalTable: "Notes",
                        principalColumn: "IdNote",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Note_User",
                columns: table => new
                {
                    IdUser = table.Column<Guid>(type: "TEXT", nullable: false),
                    IdNote = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Note_User", x => new { x.IdUser, x.IdNote });
                    table.ForeignKey(
                        name: "FK_Note_User_Notes_IdNote",
                        column: x => x.IdNote,
                        principalTable: "Notes",
                        principalColumn: "IdNote",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Note_User_User_IdUser",
                        column: x => x.IdUser,
                        principalTable: "User",
                        principalColumn: "IdUser",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncQueue",
                columns: table => new
                {
                    IdSync = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdNote = table.Column<Guid>(type: "TEXT", nullable: false),
                    IdDevice = table.Column<Guid>(type: "TEXT", nullable: false),
                    IdUser = table.Column<Guid>(type: "TEXT", nullable: false),
                    Operation = table.Column<string>(type: "TEXT", nullable: false),
                    ContentChanges = table.Column<string>(type: "TEXT", nullable: false),
                    LastUpdate = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncQueue", x => x.IdSync);
                    table.ForeignKey(
                        name: "FK_SyncQueue_Devices_IdDevice",
                        column: x => x.IdDevice,
                        principalTable: "Devices",
                        principalColumn: "IdDevice",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SyncQueue_Notes_IdNote",
                        column: x => x.IdNote,
                        principalTable: "Notes",
                        principalColumn: "IdNote",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SyncQueue_User_IdUser",
                        column: x => x.IdUser,
                        principalTable: "User",
                        principalColumn: "IdUser",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "User_Devices",
                columns: table => new
                {
                    IdUser = table.Column<Guid>(type: "TEXT", nullable: false),
                    IdDevice = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User_Devices", x => new { x.IdUser, x.IdDevice });
                    table.ForeignKey(
                        name: "FK_User_Devices_Devices_IdDevice",
                        column: x => x.IdDevice,
                        principalTable: "Devices",
                        principalColumn: "IdDevice",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_User_Devices_User_IdUser",
                        column: x => x.IdUser,
                        principalTable: "User",
                        principalColumn: "IdUser",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CRDTCharacters_IdNote",
                table: "CRDTCharacters",
                column: "IdNote");

            migrationBuilder.CreateIndex(
                name: "IX_Note_User_IdNote",
                table: "Note_User",
                column: "IdNote");

            migrationBuilder.CreateIndex(
                name: "IX_SyncQueue_IdDevice",
                table: "SyncQueue",
                column: "IdDevice");

            migrationBuilder.CreateIndex(
                name: "IX_SyncQueue_IdNote",
                table: "SyncQueue",
                column: "IdNote");

            migrationBuilder.CreateIndex(
                name: "IX_SyncQueue_IdUser",
                table: "SyncQueue",
                column: "IdUser");

            migrationBuilder.CreateIndex(
                name: "IX_User_Devices_IdDevice",
                table: "User_Devices",
                column: "IdDevice");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CRDTCharacters");

            migrationBuilder.DropTable(
                name: "Note_User");

            migrationBuilder.DropTable(
                name: "SyncQueue");

            migrationBuilder.DropTable(
                name: "User_Devices");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
