using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoodGovernanceApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBarangayHouseholdNoToConsolidatedTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_budget_allocations_users_allocated_by_id",
                table: "budget_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_master_budget_users_created_by_id",
                table: "master_budget");

            migrationBuilder.DropForeignKey(
                name: "FK_officeallocations_tbl_offices_office_code",
                table: "officeallocations");

            migrationBuilder.DropForeignKey(
                name: "FK_transactions_categories_CategoryId",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_CategoryId",
                table: "transactions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_tbl_offices_office_code",
                table: "tbl_offices");

            migrationBuilder.DropIndex(
                name: "IX_officeallocations_office_code",
                table: "officeallocations");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "transactions");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "transactions",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "tbl_program_provision",
                newName: "program");

            migrationBuilder.RenameColumn(
                name: "total_amount",
                table: "master_budget",
                newName: "total_budget");

            migrationBuilder.RenameColumn(
                name: "fiscal_year",
                table: "master_budget",
                newName: "budget_year");

            migrationBuilder.RenameColumn(
                name: "created_by_id",
                table: "master_budget",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_master_budget_created_by_id",
                table: "master_budget",
                newName: "IX_master_budget_created_by");

            migrationBuilder.RenameColumn(
                name: "allocated_by_id",
                table: "budget_allocations",
                newName: "allocated_by");

            migrationBuilder.RenameColumn(
                name: "allocated_amount",
                table: "budget_allocations",
                newName: "amount");

            migrationBuilder.RenameIndex(
                name: "IX_budget_allocations_allocated_by_id",
                table: "budget_allocations",
                newName: "IX_budget_allocations_allocated_by");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "yearlybudgets",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "yearlybudgets",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "validate_users",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "users",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AlterColumn<DateTime>(
                name: "date",
                table: "transactions",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "transactions",
                type: "decimal(65,30)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "transactions",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "transactions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "tbl_transaction",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "tbl_services",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "tbl_program_provision",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "office_code",
                table: "tbl_offices",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "tbl_offices",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "project_details",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "parameters",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "parameters",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "office_code",
                table: "officeallocations",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "officeallocations",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<long>(
                name: "office_id",
                table: "officeallocations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "officeallocations",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "budget_year",
                table: "master_budget",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "master_budget",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AlterColumn<DateTime>(
                name: "transaction_date",
                table: "consolidated_transactions",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "consolidated_transactions",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "barangay",
                table: "consolidated_transactions",
                type: "varchar(45)",
                maxLength: 45,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "household_no",
                table: "consolidated_transactions",
                type: "varchar(45)",
                maxLength: 45,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "consolidated_transactions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "categories",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "categories",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "budget_allocations",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncId",
                table: "audit_trails",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "crs_beneficiary_cache",
                columns: table => new
                {
                    beneficiary_cache_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    beneficiary_id = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    full_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    first_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    middle_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sex = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    age = table.Column<int>(type: "int", nullable: true),
                    address = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    marital_status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_pwd = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_senior = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    cached_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crs_beneficiary_cache", x => x.beneficiary_cache_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_officeallocations_office_id",
                table: "officeallocations",
                column: "office_id");

            migrationBuilder.AddForeignKey(
                name: "FK_budget_allocations_users_allocated_by",
                table: "budget_allocations",
                column: "allocated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_master_budget_users_created_by",
                table: "master_budget",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_officeallocations_tbl_offices_office_id",
                table: "officeallocations",
                column: "office_id",
                principalTable: "tbl_offices",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_budget_allocations_users_allocated_by",
                table: "budget_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_master_budget_users_created_by",
                table: "master_budget");

            migrationBuilder.DropForeignKey(
                name: "FK_officeallocations_tbl_offices_office_id",
                table: "officeallocations");

            migrationBuilder.DropTable(
                name: "crs_beneficiary_cache");

            migrationBuilder.DropIndex(
                name: "IX_officeallocations_office_id",
                table: "officeallocations");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "yearlybudgets");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "yearlybudgets");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "validate_users");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "tbl_transaction");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "tbl_services");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "tbl_program_provision");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "tbl_offices");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "project_details");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "parameters");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "parameters");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "officeallocations");

            migrationBuilder.DropColumn(
                name: "office_id",
                table: "officeallocations");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "officeallocations");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "master_budget");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "consolidated_transactions");

            migrationBuilder.DropColumn(
                name: "barangay",
                table: "consolidated_transactions");

            migrationBuilder.DropColumn(
                name: "household_no",
                table: "consolidated_transactions");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "consolidated_transactions");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "budget_allocations");

            migrationBuilder.DropColumn(
                name: "SyncId",
                table: "audit_trails");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "transactions",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "program",
                table: "tbl_program_provision",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "total_budget",
                table: "master_budget",
                newName: "total_amount");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "master_budget",
                newName: "created_by_id");

            migrationBuilder.RenameColumn(
                name: "budget_year",
                table: "master_budget",
                newName: "fiscal_year");

            migrationBuilder.RenameIndex(
                name: "IX_master_budget_created_by",
                table: "master_budget",
                newName: "IX_master_budget_created_by_id");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "budget_allocations",
                newName: "allocated_amount");

            migrationBuilder.RenameColumn(
                name: "allocated_by",
                table: "budget_allocations",
                newName: "allocated_by_id");

            migrationBuilder.RenameIndex(
                name: "IX_budget_allocations_allocated_by",
                table: "budget_allocations",
                newName: "IX_budget_allocations_allocated_by_id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "transactions",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "transactions",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "transactions",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "tbl_offices",
                keyColumn: "office_code",
                keyValue: null,
                column: "office_code",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "office_code",
                table: "tbl_offices",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "officeallocations",
                keyColumn: "office_code",
                keyValue: null,
                column: "office_code",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "office_code",
                table: "officeallocations",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "fiscal_year",
                table: "master_budget",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "transaction_date",
                table: "consolidated_transactions",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_tbl_offices_office_code",
                table: "tbl_offices",
                column: "office_code");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_CategoryId",
                table: "transactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_officeallocations_office_code",
                table: "officeallocations",
                column: "office_code");

            migrationBuilder.AddForeignKey(
                name: "FK_budget_allocations_users_allocated_by_id",
                table: "budget_allocations",
                column: "allocated_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_master_budget_users_created_by_id",
                table: "master_budget",
                column: "created_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_officeallocations_tbl_offices_office_code",
                table: "officeallocations",
                column: "office_code",
                principalTable: "tbl_offices",
                principalColumn: "office_code",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_categories_CategoryId",
                table: "transactions",
                column: "CategoryId",
                principalTable: "categories",
                principalColumn: "Id");
        }
    }
}
