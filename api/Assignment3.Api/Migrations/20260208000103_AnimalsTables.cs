using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Assignment3.Api.Migrations
{
    /// <inheritdoc />
    public partial class AnimalsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Scores");

            migrationBuilder.CreateTable(
                name: "AnimalTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Attack = table.Column<int>(type: "int", nullable: false),
                    Defense = table.Column<int>(type: "int", nullable: false),
                    Affection = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    HpMax = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerAnimals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerPlayerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Attack = table.Column<int>(type: "int", nullable: false),
                    Defense = table.Column<int>(type: "int", nullable: false),
                    Affection = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    HpMax = table.Column<int>(type: "int", nullable: false),
                    HpCurrent = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerAnimals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerAnimals_AnimalTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "AnimalTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AnimalTemplates",
                columns: new[] { "Id", "Affection", "Attack", "Defense", "HpMax", "Kind", "Level" },
                values: new object[,]
                {
                    { 1, 3, 5, 4, 30, "dog", 1 },
                    { 2, 4, 6, 3, 28, "cat", 1 },
                    { 3, 2, 4, 5, 32, "hamster", 1 },
                    { 4, 2, 8, 5, 34, "fox", 1 },
                    { 5, 2, 7, 6, 36, "owl", 1 },
                    { 6, 1, 6, 8, 40, "boar", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalTemplates_Kind",
                table: "AnimalTemplates",
                column: "Kind",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAnimals_TemplateId",
                table: "PlayerAnimals",
                column: "TemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerAnimals");

            migrationBuilder.DropTable(
                name: "AnimalTemplates");

            migrationBuilder.CreateTable(
                name: "Scores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scores", x => x.Id);
                });
        }
    }
}
