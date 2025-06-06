using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WpfApp2.Migrations
{
    /// <inheritdoc />
    public partial class AddedLLM_Detail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LLM_Detail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalUserQuery = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessedUserQuery = table.Column<string>(type: "TEXT", nullable: false),
                    PrimaryIntent = table.Column<string>(type: "TEXT", nullable: false),
                    SpecificAction = table.Column<string>(type: "TEXT", nullable: false),
                    FileDescription = table.Column<string>(type: "TEXT", nullable: false),
                    FileTypeFilter = table.Column<string>(type: "TEXT", nullable: false),
                    ApplicationName = table.Column<string>(type: "TEXT", nullable: false),
                    SearchEngineQuery = table.Column<string>(type: "TEXT", nullable: false),
                    SystemComponentTarget = table.Column<string>(type: "TEXT", nullable: false),
                    SystemComponentValue = table.Column<string>(type: "TEXT", nullable: false),
                    TaskDescriptionForSchedule = table.Column<string>(type: "TEXT", nullable: false),
                    ScheduleDatetimeDescription = table.Column<string>(type: "TEXT", nullable: false),
                    SystemCommand = table.Column<string>(type: "TEXT", nullable: false),
                    TimeReferences = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LLM_Detail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LLM_Detail_SignUpDetails_UserId",
                        column: x => x.UserId,
                        principalTable: "SignUpDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LLM_Detail_UserId",
                table: "LLM_Detail",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LLM_Detail");
        }
    }
}
