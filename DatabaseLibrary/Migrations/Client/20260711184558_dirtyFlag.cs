using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabaseLibrary.Migrations.Client
{
    /// <inheritdoc />
    public partial class dirtyFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DirtyFlagChangesMade",
                table: "Notes");

            migrationBuilder.RenameColumn(
                name: "HasPassword",
                table: "Notes",
                newName: "hasPassword");

            migrationBuilder.RenameColumn(
                name: "CreationDate",
                table: "Notes",
                newName: "StartingDate");
        }
    }
}
