using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WpfApp2.Migrations
{
    /// <inheritdoc />
    public partial class AddedLLM_Detail1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationName",
                table: "LLM_Detail");

            migrationBuilder.DropColumn(
                name: "FileDescription",
                table: "LLM_Detail");

            migrationBuilder.DropColumn(
                name: "FileTypeFilter",
                table: "LLM_Detail");

            migrationBuilder.DropColumn(
                name: "OriginalUserQuery",
                table: "LLM_Detail");

            migrationBuilder.DropColumn(
                name: "PrimaryIntent",
                table: "LLM_Detail");

            migrationBuilder.DropColumn(
                name: "ProcessedUserQuery",
                table: "LLM_Detail");

            migrationBuilder.DropColumn(
                name: "ScheduleDatetimeDescription",
                table: "LLM_Detail");

            migrationBuilder.DropColumn(
                name: "SearchEngineQuery",
                table: "LLM_Detail");

            migrationBuilder.DropColumn(
                name: "SpecificAction",
                table: "LLM_Detail");

            migrationBuilder.DropColumn(
                name: "SystemCommand",
                table: "LLM_Detail");

            migrationBuilder.DropColumn(
                name: "SystemComponentTarget",
                table: "LLM_Detail");

            migrationBuilder.DropColumn(
                name: "SystemComponentValue",
                table: "LLM_Detail");

            migrationBuilder.DropColumn(
                name: "TaskDescriptionForSchedule",
                table: "LLM_Detail");

            migrationBuilder.DropColumn(
                name: "TimeReferences",
                table: "LLM_Detail");

            migrationBuilder.AddColumn<string>(
                name: "Expected_json",
                table: "LLM_Detail",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Expected_json",
                table: "LLM_Detail");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationName",
                table: "LLM_Detail",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileDescription",
                table: "LLM_Detail",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileTypeFilter",
                table: "LLM_Detail",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OriginalUserQuery",
                table: "LLM_Detail",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryIntent",
                table: "LLM_Detail",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProcessedUserQuery",
                table: "LLM_Detail",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ScheduleDatetimeDescription",
                table: "LLM_Detail",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SearchEngineQuery",
                table: "LLM_Detail",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SpecificAction",
                table: "LLM_Detail",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SystemCommand",
                table: "LLM_Detail",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SystemComponentTarget",
                table: "LLM_Detail",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SystemComponentValue",
                table: "LLM_Detail",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaskDescriptionForSchedule",
                table: "LLM_Detail",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TimeReferences",
                table: "LLM_Detail",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
