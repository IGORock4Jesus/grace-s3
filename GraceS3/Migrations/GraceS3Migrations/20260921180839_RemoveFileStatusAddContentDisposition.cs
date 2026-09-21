using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraceS3.Migrations.GraceS3Migrations
{
    /// <inheritdoc />
    public partial class RemoveFileStatusAddContentDisposition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileStatus",
                table: "Objects");

            migrationBuilder.AddColumn<string>(
                name: "ContentDisposition",
                table: "Objects",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentDisposition",
                table: "Objects");

            migrationBuilder.AddColumn<string>(
                name: "FileStatus",
                table: "Objects",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
