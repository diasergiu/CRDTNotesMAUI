using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabaseLibrary.Migrations.Server
{
    /// <inheritdoc />
    public partial class dirtyFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServerNoteUsers_ServerNotes_IdNote",
                table: "ServerNoteUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_ServerNoteUsers_UserClient_IdUser",
                table: "ServerNoteUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_UserDevices_Devices_IdDevice",
                table: "UserDevices");

            migrationBuilder.DropForeignKey(
                name: "FK_UserDevices_UserClient_IdUser",
                table: "UserDevices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserDevices",
                table: "UserDevices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServerNoteUsers",
                table: "ServerNoteUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServerNotes",
                table: "ServerNotes");

            migrationBuilder.RenameTable(
                name: "UserDevices",
                newName: "User_Devices");

            migrationBuilder.RenameTable(
                name: "ServerNoteUsers",
                newName: "Note_Users");

            migrationBuilder.RenameTable(
                name: "ServerNotes",
                newName: "Notes");

            migrationBuilder.RenameIndex(
                name: "IX_UserDevices_IdDevice",
                table: "User_Devices",
                newName: "IX_User_Devices_IdDevice");

            migrationBuilder.RenameIndex(
                name: "IX_ServerNoteUsers_IdNote",
                table: "Note_Users",
                newName: "IX_Note_Users_IdNote");

            migrationBuilder.RenameColumn(
                name: "hasPassword",
                table: "Notes",
                newName: "HasPassword");

            migrationBuilder.RenameColumn(
                name: "StartingDate",
                table: "Notes",
                newName: "CreationDate");

            migrationBuilder.AddColumn<bool>(
                name: "DirtyFlagChangesMade",
                table: "Notes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_User_Devices",
                table: "User_Devices",
                columns: new[] { "IdUser", "IdDevice" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Note_Users",
                table: "Note_Users",
                columns: new[] { "IdUser", "IdNote" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Notes",
                table: "Notes",
                column: "IdNote");

            migrationBuilder.AddForeignKey(
                name: "FK_Note_Users_Notes_IdNote",
                table: "Note_Users",
                column: "IdNote",
                principalTable: "Notes",
                principalColumn: "IdNote",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Note_Users_UserClient_IdUser",
                table: "Note_Users",
                column: "IdUser",
                principalTable: "UserClient",
                principalColumn: "IdUser",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_User_Devices_Devices_IdDevice",
                table: "User_Devices",
                column: "IdDevice",
                principalTable: "Devices",
                principalColumn: "IdDevice",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_User_Devices_UserClient_IdUser",
                table: "User_Devices",
                column: "IdUser",
                principalTable: "UserClient",
                principalColumn: "IdUser",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Note_Users_Notes_IdNote",
                table: "Note_Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Note_Users_UserClient_IdUser",
                table: "Note_Users");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Devices_Devices_IdDevice",
                table: "User_Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Devices_UserClient_IdUser",
                table: "User_Devices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_User_Devices",
                table: "User_Devices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Notes",
                table: "Notes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Note_Users",
                table: "Note_Users");

            migrationBuilder.DropColumn(
                name: "DirtyFlagChangesMade",
                table: "Notes");

            migrationBuilder.RenameTable(
                name: "User_Devices",
                newName: "UserDevices");

            migrationBuilder.RenameTable(
                name: "Notes",
                newName: "ServerNotes");

            migrationBuilder.RenameTable(
                name: "Note_Users",
                newName: "ServerNoteUsers");

            migrationBuilder.RenameIndex(
                name: "IX_User_Devices_IdDevice",
                table: "UserDevices",
                newName: "IX_UserDevices_IdDevice");

            migrationBuilder.RenameColumn(
                name: "HasPassword",
                table: "ServerNotes",
                newName: "hasPassword");

            migrationBuilder.RenameColumn(
                name: "CreationDate",
                table: "ServerNotes",
                newName: "StartingDate");

            migrationBuilder.RenameIndex(
                name: "IX_Note_Users_IdNote",
                table: "ServerNoteUsers",
                newName: "IX_ServerNoteUsers_IdNote");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserDevices",
                table: "UserDevices",
                columns: new[] { "IdUser", "IdDevice" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServerNotes",
                table: "ServerNotes",
                column: "IdNote");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServerNoteUsers",
                table: "ServerNoteUsers",
                columns: new[] { "IdUser", "IdNote" });

            migrationBuilder.AddForeignKey(
                name: "FK_ServerNoteUsers_ServerNotes_IdNote",
                table: "ServerNoteUsers",
                column: "IdNote",
                principalTable: "ServerNotes",
                principalColumn: "IdNote",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServerNoteUsers_UserClient_IdUser",
                table: "ServerNoteUsers",
                column: "IdUser",
                principalTable: "UserClient",
                principalColumn: "IdUser",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserDevices_Devices_IdDevice",
                table: "UserDevices",
                column: "IdDevice",
                principalTable: "Devices",
                principalColumn: "IdDevice",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserDevices_UserClient_IdUser",
                table: "UserDevices",
                column: "IdUser",
                principalTable: "UserClient",
                principalColumn: "IdUser",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
