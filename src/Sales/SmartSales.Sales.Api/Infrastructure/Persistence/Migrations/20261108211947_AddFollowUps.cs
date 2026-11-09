using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSales.Sales.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFollowUps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "follow_ups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opportunity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_follow_ups", x => x.id);
                    table.CheckConstraint("ck_follow_ups_completion", "(status = 'Pending' AND completed_at IS NULL AND completed_by_user_id IS NULL) OR (status = 'Completed' AND completed_at IS NOT NULL AND completed_by_user_id IS NOT NULL)");
                    table.CheckConstraint("ck_follow_ups_status", "status IN ('Pending', 'Completed')");
                    table.CheckConstraint("ck_follow_ups_version", "version >= 1");
                    table.ForeignKey(
                        name: "FK_follow_ups_opportunities_opportunity_id_contact_id",
                        columns: x => new { x.opportunity_id, x.contact_id },
                        principalTable: "opportunities",
                        principalColumns: new[] { "id", "contact_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_follow_ups_contact_id_scheduled_at_id",
                table: "follow_ups",
                columns: new[] { "contact_id", "scheduled_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_follow_ups_opportunity_id_contact_id",
                table: "follow_ups",
                columns: new[] { "opportunity_id", "contact_id" });

            migrationBuilder.CreateIndex(
                name: "IX_follow_ups_owner_user_id_status_scheduled_at_id",
                table: "follow_ups",
                columns: new[] { "owner_user_id", "status", "scheduled_at", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "follow_ups");
        }
    }
}
