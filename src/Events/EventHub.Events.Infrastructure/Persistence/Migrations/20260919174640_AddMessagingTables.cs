using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Events.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddMessagingTables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "inbox_messages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_inbox_messages", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "outbox_messages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                topic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                payload = table.Column<string>(type: "jsonb", nullable: false),
                occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_outbox_messages", x => x.id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "inbox_messages");

        migrationBuilder.DropTable(
            name: "outbox_messages");
    }
}
