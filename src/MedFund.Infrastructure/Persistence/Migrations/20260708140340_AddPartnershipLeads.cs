using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedFund.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnershipLeads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "partnership_leads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    partner_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    organization_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "NEW"),
                    source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "WEBSITE_PARTNERSHIP_FORM"),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    contacted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partnership_leads", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_partnership_leads_created_at",
                table: "partnership_leads",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_partnership_leads_email",
                table: "partnership_leads",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "idx_partnership_leads_partner_type",
                table: "partnership_leads",
                column: "partner_type");

            migrationBuilder.CreateIndex(
                name: "idx_partnership_leads_status",
                table: "partnership_leads",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "partnership_leads");
        }
    }
}
