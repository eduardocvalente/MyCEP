using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCEP.API.Migrations
{
    /// <inheritdoc />
    public partial class AddZipcodeUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dados legados podem ter CEP com hífen (os seeds originais tinham),
            // enquanto a aplicação sempre busca por somente dígitos — essas
            // linhas eram inalcançáveis. Normaliza antes de criar o índice.
            migrationBuilder.Sql(
                "UPDATE address SET zipcode = regexp_replace(zipcode, '[^0-9]', '', 'g') WHERE zipcode ~ '[^0-9]';");

            // Sem índice único, duas requisições simultâneas do mesmo CEP
            // inseriam linhas duplicadas. Remove os duplicados existentes
            // preservando o registro mais antigo de cada CEP.
            migrationBuilder.Sql(@"
                DELETE FROM address
                WHERE id NOT IN (
                    SELECT DISTINCT ON (zipcode) id
                    FROM address
                    ORDER BY zipcode, created_at, id
                );");

            // Índice único parcial: a unicidade do CEP vale apenas entre
            // registros ativos (deleted_at IS NULL), casando com o filtro
            // global de soft delete.
            migrationBuilder.CreateIndex(
                name: "ix_address_zipcode",
                table: "address",
                column: "zipcode",
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_address_zipcode",
                table: "address");
        }
    }
}
