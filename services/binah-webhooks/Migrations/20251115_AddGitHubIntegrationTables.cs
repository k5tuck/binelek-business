using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Binah.Webhooks.Migrations
{
    /// <inheritdoc />
    public partial class AddGitHubIntegrationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // GitHub Webhook Events table
            migrationBuilder.CreateTable(
                name: "github_webhook_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    repository_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    signature = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    processed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_github_webhook_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_github_webhook_events_tenant_id",
                table: "github_webhook_events",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_github_webhook_events_event_type",
                table: "github_webhook_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "IX_github_webhook_events_received_at",
                table: "github_webhook_events",
                column: "received_at");

            // GitHub OAuth Tokens table
            migrationBuilder.CreateTable(
                name: "github_oauth_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    token_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Bearer"),
                    scope = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    refresh_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_github_oauth_tokens", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_github_oauth_tokens_tenant_id",
                table: "github_oauth_tokens",
                column: "tenant_id",
                unique: true);

            // Autonomous Pull Requests table
            migrationBuilder.CreateTable(
                name: "autonomous_pull_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pr_number = table.Column<int>(type: "integer", nullable: false),
                    repository_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    branch_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    workflow_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "open"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    merged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_autonomous_pull_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_autonomous_pull_requests_tenant_id",
                table: "autonomous_pull_requests",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_autonomous_pull_requests_status",
                table: "autonomous_pull_requests",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "github_webhook_events");
            migrationBuilder.DropTable(name: "github_oauth_tokens");
            migrationBuilder.DropTable(name: "autonomous_pull_requests");
        }
    }
}
