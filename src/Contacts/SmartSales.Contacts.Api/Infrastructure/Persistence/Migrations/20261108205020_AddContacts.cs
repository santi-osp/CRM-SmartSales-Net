using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSales.Contacts.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    normalized_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contacts", x => x.id);
                    table.CheckConstraint("ck_contacts_channel", "normalized_email IS NOT NULL OR normalized_phone IS NOT NULL");
                    table.CheckConstraint("ck_contacts_source", "source IN ('Web', 'Referral', 'WhatsApp', 'Call', 'Other')");
                    table.CheckConstraint("ck_contacts_type", "type IN ('Prospect', 'Customer')");
                    table.CheckConstraint("ck_contacts_version", "version >= 1");
                });

            migrationBuilder.CreateIndex(
                name: "IX_contacts_created_at_id",
                table: "contacts",
                columns: new[] { "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_contacts_normalized_email",
                table: "contacts",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_normalized_phone",
                table: "contacts",
                column: "normalized_phone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contacts");
        }
    }
}
