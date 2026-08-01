using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MyCEP.API.Migrations
{
    /// <inheritdoc />
    public partial class firstMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "address",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    zipcode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    complement = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    neighborhood = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    state = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    ibgeCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    giaCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    areaCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    siafiCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_address", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "address",
                columns: new[] { "id", "city", "complement", "created_at", "areaCode", "deleted_at", "giaCode", "ibgeCode", "neighborhood", "siafiCode", "state", "street", "updated_at", "zipcode" },
                values: new object[,]
                {
                    { new Guid("b3b5b50f-8c8f-4f3b-a8fa-cf6e03750df1"), "São Paulo", "Apto 101", new DateTimeOffset(new DateTime(2024, 2, 26, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "11", null, "1004", "3550308", "Bela Vista", "7107", "SP", "Avenida Paulista", null, "01000-000" },
                    { new Guid("bba8c410-1f3a-4b5c-93c3-8317ebaa7f9e"), "Rio de Janeiro", "Sala 202", new DateTimeOffset(new DateTime(2024, 2, 26, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "21", null, "2001", "3304557", "Centro", "6001", "RJ", "Rua da Assembleia", null, "20000-000" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "address");
        }
    }
}
