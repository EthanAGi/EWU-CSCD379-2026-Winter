using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MortuaryAssistant.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedMorticianToCaseFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedMorticianId",
                table: "CaseFiles",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedMorticianUserId",
                table: "CaseFiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseFiles_AssignedMorticianId",
                table: "CaseFiles",
                column: "AssignedMorticianId");

            migrationBuilder.AddForeignKey(
                name: "FK_CaseFiles_AspNetUsers_AssignedMorticianId",
                table: "CaseFiles",
                column: "AssignedMorticianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaseFiles_AspNetUsers_AssignedMorticianId",
                table: "CaseFiles");

            migrationBuilder.DropIndex(
                name: "IX_CaseFiles_AssignedMorticianId",
                table: "CaseFiles");

            migrationBuilder.DropColumn(
                name: "AssignedMorticianId",
                table: "CaseFiles");

            migrationBuilder.DropColumn(
                name: "AssignedMorticianUserId",
                table: "CaseFiles");
        }
    }
}
