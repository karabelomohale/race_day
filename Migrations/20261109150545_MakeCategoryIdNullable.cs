using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace race_day.Migrations
{
    /// <inheritdoc />
    public partial class MakeCategoryIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventEnrolments_Categories_CategoryId",
                table: "EventEnrolments");

            migrationBuilder.DropIndex(
                name: "IX_EventEnrolments_CategoryId_ParticipantId",
                table: "EventEnrolments");

            migrationBuilder.DropIndex(
                name: "IX_EventEnrolments_CategoryId_RaceNumber",
                table: "EventEnrolments");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "EventEnrolments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_EventEnrolments_CategoryId_ParticipantId",
                table: "EventEnrolments",
                columns: new[] { "CategoryId", "ParticipantId" },
                unique: true,
                filter: "[CategoryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventEnrolments_CategoryId_RaceNumber",
                table: "EventEnrolments",
                columns: new[] { "CategoryId", "RaceNumber" },
                unique: true,
                filter: "[CategoryId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_EventEnrolments_Categories_CategoryId",
                table: "EventEnrolments",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventEnrolments_Categories_CategoryId",
                table: "EventEnrolments");

            migrationBuilder.DropIndex(
                name: "IX_EventEnrolments_CategoryId_ParticipantId",
                table: "EventEnrolments");

            migrationBuilder.DropIndex(
                name: "IX_EventEnrolments_CategoryId_RaceNumber",
                table: "EventEnrolments");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "EventEnrolments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventEnrolments_CategoryId_ParticipantId",
                table: "EventEnrolments",
                columns: new[] { "CategoryId", "ParticipantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventEnrolments_CategoryId_RaceNumber",
                table: "EventEnrolments",
                columns: new[] { "CategoryId", "RaceNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EventEnrolments_Categories_CategoryId",
                table: "EventEnrolments",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
