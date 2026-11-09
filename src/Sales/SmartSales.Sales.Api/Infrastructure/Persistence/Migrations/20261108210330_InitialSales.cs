using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSales.Sales.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "opportunities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    estimated_amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    expected_close_date = table.Column<DateOnly>(type: "date", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    priority = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    stage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    final_amount = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    loss_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opportunities", x => x.id);
                    table.UniqueConstraint("AK_opportunities_id_contact_id", x => new { x.id, x.contact_id });
                    table.CheckConstraint("ck_opportunities_amount", "estimated_amount >= 0");
                    table.CheckConstraint("ck_opportunities_closed", "(stage IN ('New','Contacted','Proposal') AND closed_at IS NULL AND closed_by_user_id IS NULL AND final_amount IS NULL AND loss_reason IS NULL) OR (stage = 'Won' AND closed_at IS NOT NULL AND closed_by_user_id IS NOT NULL AND final_amount IS NOT NULL AND final_amount >= 0 AND loss_reason IS NULL) OR (stage = 'Lost' AND closed_at IS NOT NULL AND closed_by_user_id IS NOT NULL AND final_amount IS NULL AND loss_reason IS NOT NULL AND length(trim(loss_reason)) > 0)");
                    table.CheckConstraint("ck_opportunities_priority", "priority IN ('High', 'Medium', 'Low')");
                    table.CheckConstraint("ck_opportunities_stage", "stage IN ('New', 'Contacted', 'Proposal', 'Won', 'Lost')");
                    table.CheckConstraint("ck_opportunities_version", "version >= 1");
                });

            migrationBuilder.CreateTable(
                name: "opportunity_owner_changes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    opportunity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    new_owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opportunity_owner_changes", x => x.id);
                    table.CheckConstraint("ck_owner_changed", "previous_owner_user_id <> new_owner_user_id");
                    table.ForeignKey(
                        name: "FK_opportunity_owner_changes_opportunities_opportunity_id",
                        column: x => x.opportunity_id,
                        principalTable: "opportunities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_opportunities_contact_id_created_at_id",
                table: "opportunities",
                columns: new[] { "contact_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_opportunities_owner_user_id_priority_created_at_id",
                table: "opportunities",
                columns: new[] { "owner_user_id", "priority", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_opportunities_stage_created_at_id",
                table: "opportunities",
                columns: new[] { "stage", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_opportunity_owner_changes_opportunity_id_changed_at_id",
                table: "opportunity_owner_changes",
                columns: new[] { "opportunity_id", "changed_at", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "opportunity_owner_changes");

            migrationBuilder.DropTable(
                name: "opportunities");
        }
    }
}
