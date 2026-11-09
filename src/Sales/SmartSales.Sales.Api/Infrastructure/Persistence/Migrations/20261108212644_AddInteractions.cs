using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSales.Sales.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInteractions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "interactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opportunity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interactions", x => x.id);
                    table.CheckConstraint("ck_interactions_type", "type IN ('Call', 'Email', 'Meeting')");
                    table.ForeignKey(
                        name: "FK_interactions_opportunities_opportunity_id_contact_id",
                        columns: x => new { x.opportunity_id, x.contact_id },
                        principalTable: "opportunities",
                        principalColumns: new[] { "id", "contact_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_interactions_contact_id_occurred_at_id",
                table: "interactions",
                columns: new[] { "contact_id", "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_interactions_opportunity_id_contact_id",
                table: "interactions",
                columns: new[] { "opportunity_id", "contact_id" });

            migrationBuilder.CreateIndex(
                name: "IX_interactions_opportunity_id_occurred_at_id",
                table: "interactions",
                columns: new[] { "opportunity_id", "occurred_at", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "interactions");
        }
    }
}
