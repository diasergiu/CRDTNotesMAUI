using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabaseLibrary.Migrations.Server
{
    /// <inheritdoc />
    public partial class syncQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Note_Users_UserClient_IdUser",
                table: "Note_Users");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Devices_UserClient_IdUser",
                table: "User_Devices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserClient",
                table: "UserClient");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "UserClient");

            migrationBuilder.RenameTable(
                name: "UserClient",
                newName: "User");

            migrationBuilder.AddPrimaryKey(
                name: "PK_User",
                table: "User",
                column: "IdUser");

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    IdUser = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.IdUser);
                });

            migrationBuilder.CreateTable(
                name: "SyncQueue",
                columns: table => new
                {
                    IdSync = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdNote = table.Column<int>(type: "INTEGER", nullable: false),
                    IdDevice = table.Column<int>(type: "INTEGER", nullable: false),
                    IdUser = table.Column<int>(type: "INTEGER", nullable: false),
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
                        name: "FK_SyncQueue_user_IdUser",
                        column: x => x.IdUser,
                        principalTable: "user",
                        principalColumn: "IdUser",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.AddForeignKey(
                name: "FK_Note_Users_User_IdUser",
                table: "Note_Users",
                column: "IdUser",
                principalTable: "User",
                principalColumn: "IdUser",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_User_Devices_user_IdUser",
                table: "User_Devices",
                column: "IdUser",
                principalTable: "user",
                principalColumn: "IdUser",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Note_Users_User_IdUser",
                table: "Note_Users");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Devices_user_IdUser",
                table: "User_Devices");

            migrationBuilder.DropTable(
                name: "SyncQueue");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropPrimaryKey(
                name: "PK_User",
                table: "User");

            migrationBuilder.RenameTable(
                name: "User",
                newName: "UserClient");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "UserClient",
                type: "TEXT",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserClient",
                table: "UserClient",
                column: "IdUser");

            migrationBuilder.AddForeignKey(
                name: "FK_Note_Users_UserClient_IdUser",
                table: "Note_Users",
                column: "IdUser",
                principalTable: "UserClient",
                principalColumn: "IdUser",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_User_Devices_UserClient_IdUser",
                table: "User_Devices",
                column: "IdUser",
                principalTable: "UserClient",
                principalColumn: "IdUser",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
