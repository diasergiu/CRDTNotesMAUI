using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabaseLibrary.Migrations.DbContextClientMigrations
{
    /// <inheritdoc />
    public partial class addCRDTCharacterCompositeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CRDTCharacter_CRDTCharacter_IdLeftCharacter",
                table: "CRDTCharacter");

            migrationBuilder.DropForeignKey(
                name: "FK_CRDTCharacter_CRDTCharacter_IdRightCharacter",
                table: "CRDTCharacter");

            migrationBuilder.DropForeignKey(
                name: "FK_CRDTCharacter_Note_IdNote",
                table: "CRDTCharacter");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CRDTCharacter",
                table: "CRDTCharacter");

            migrationBuilder.DropIndex(
                name: "IX_CRDTCharacter_IdLeftCharacter",
                table: "CRDTCharacter");

            migrationBuilder.DropIndex(
                name: "IX_CRDTCharacter_IdRightCharacter",
                table: "CRDTCharacter");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "CRDTCharacter");

            migrationBuilder.RenameTable(
                name: "CRDTCharacter",
                newName: "CRDTCharacters");

            migrationBuilder.RenameIndex(
                name: "IX_CRDTCharacter_IdNote",
                table: "CRDTCharacters",
                newName: "IX_CRDTCharacters_IdNote");

            migrationBuilder.AlterColumn<int>(
                name: "IdCharacter",
                table: "CRDTCharacters",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CRDTCharacters",
                table: "CRDTCharacters",
                columns: new[] { "IdCharacter", "IdNote" });

            migrationBuilder.AddForeignKey(
                name: "FK_CRDTCharacters_Note_IdNote",
                table: "CRDTCharacters",
                column: "IdNote",
                principalTable: "Note",
                principalColumn: "IdNote",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CRDTCharacters_Note_IdNote",
                table: "CRDTCharacters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CRDTCharacters",
                table: "CRDTCharacters");

            migrationBuilder.RenameTable(
                name: "CRDTCharacters",
                newName: "CRDTCharacter");

            migrationBuilder.RenameIndex(
                name: "IX_CRDTCharacters_IdNote",
                table: "CRDTCharacter",
                newName: "IX_CRDTCharacter_IdNote");

            migrationBuilder.AlterColumn<int>(
                name: "IdCharacter",
                table: "CRDTCharacter",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "CRDTCharacter",
                type: "TEXT",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CRDTCharacter",
                table: "CRDTCharacter",
                column: "IdCharacter");

            migrationBuilder.CreateIndex(
                name: "IX_CRDTCharacter_IdLeftCharacter",
                table: "CRDTCharacter",
                column: "IdLeftCharacter");

            migrationBuilder.CreateIndex(
                name: "IX_CRDTCharacter_IdRightCharacter",
                table: "CRDTCharacter",
                column: "IdRightCharacter");

            migrationBuilder.AddForeignKey(
                name: "FK_CRDTCharacter_CRDTCharacter_IdLeftCharacter",
                table: "CRDTCharacter",
                column: "IdLeftCharacter",
                principalTable: "CRDTCharacter",
                principalColumn: "IdCharacter");

            migrationBuilder.AddForeignKey(
                name: "FK_CRDTCharacter_CRDTCharacter_IdRightCharacter",
                table: "CRDTCharacter",
                column: "IdRightCharacter",
                principalTable: "CRDTCharacter",
                principalColumn: "IdCharacter");

            migrationBuilder.AddForeignKey(
                name: "FK_CRDTCharacter_Note_IdNote",
                table: "CRDTCharacter",
                column: "IdNote",
                principalTable: "Note",
                principalColumn: "IdNote",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
