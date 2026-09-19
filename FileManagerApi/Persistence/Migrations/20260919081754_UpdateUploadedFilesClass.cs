using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileManagerApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUploadedFilesClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContentTybe",
                table: "Files",
                newName: "ContentType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContentType",
                table: "Files",
                newName: "ContentTybe");
        }
    }
}
