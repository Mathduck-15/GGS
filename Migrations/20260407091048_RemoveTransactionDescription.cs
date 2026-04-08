using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoodGovernanceApp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTransactionDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Budgets_Categories_CategoryId",
                table: "Budgets");

            migrationBuilder.DropForeignKey(
                name: "FK_office_allocations_YearlyBudgets_YearlyBudgetId",
                table: "office_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_office_allocations_tbl_offices_OfficeId",
                table: "office_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_Parameters_Categories_CategoryId",
                table: "Parameters");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Categories_CategoryId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_users_UserId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_UploadedFiles_Parameters_ParameterId",
                table: "UploadedFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_validate_users_Categories_category_id",
                table: "validate_users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_YearlyBudgets",
                table: "YearlyBudgets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Parameters",
                table: "Parameters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categories",
                table: "Categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_office_allocations",
                table: "office_allocations");

            migrationBuilder.DropIndex(
                name: "IX_office_allocations_OfficeId",
                table: "office_allocations");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "spent_amount",
                table: "budget_allocations");

            migrationBuilder.DropColumn(
                name: "OfficeId",
                table: "office_allocations");

            migrationBuilder.DropColumn(
                name: "SpentAmount",
                table: "office_allocations");

            migrationBuilder.RenameTable(
                name: "YearlyBudgets",
                newName: "yearlybudgets");

            migrationBuilder.RenameTable(
                name: "Transactions",
                newName: "transactions");

            migrationBuilder.RenameTable(
                name: "Parameters",
                newName: "parameters");

            migrationBuilder.RenameTable(
                name: "Categories",
                newName: "categories");

            migrationBuilder.RenameTable(
                name: "office_allocations",
                newName: "officeallocations");

            migrationBuilder.RenameColumn(
                name: "TransactionType",
                table: "transactions",
                newName: "transaction_type");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_CategoryId",
                table: "transactions",
                newName: "IX_transactions_CategoryId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "project_details",
                newName: "project");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "project_details",
                newName: "create_at");

            migrationBuilder.RenameColumn(
                name: "budget",
                table: "project_details",
                newName: "total_budget");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "project_details",
                newName: "project_details_id");

            migrationBuilder.RenameIndex(
                name: "IX_Parameters_CategoryId",
                table: "parameters",
                newName: "IX_parameters_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_office_allocations_YearlyBudgetId",
                table: "officeallocations",
                newName: "IX_officeallocations_YearlyBudgetId");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "UploadedFiles",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "transactions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "transaction_type",
                table: "transactions",
                type: "varchar(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "project_code",
                table: "transactions",
                type: "varchar(45)",
                maxLength: 45,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "voucher_code",
                table: "transactions",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "voucher_code",
                table: "tbl_transaction",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

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
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "contact_person",
                table: "project_details",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "office_code",
                table: "project_details",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "voucher_code",
                table: "project_details",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "yearly_budget_id",
                table: "project_details",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "office_code",
                table: "officeallocations",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_yearlybudgets",
                table: "yearlybudgets",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_transactions",
                table: "transactions",
                column: "Id");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_tbl_offices_office_code",
                table: "tbl_offices",
                column: "office_code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_parameters",
                table: "parameters",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_categories",
                table: "categories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_officeallocations",
                table: "officeallocations",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_CategoryId",
                table: "UploadedFiles",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_offices_office_code",
                table: "tbl_offices",
                column: "office_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_officeallocations_office_code",
                table: "officeallocations",
                column: "office_code");

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_categories_CategoryId",
                table: "Budgets",
                column: "CategoryId",
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_officeallocations_tbl_offices_office_code",
                table: "officeallocations",
                column: "office_code",
                principalTable: "tbl_offices",
                principalColumn: "office_code",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_officeallocations_yearlybudgets_YearlyBudgetId",
                table: "officeallocations",
                column: "YearlyBudgetId",
                principalTable: "yearlybudgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_parameters_categories_CategoryId",
                table: "parameters",
                column: "CategoryId",
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_categories_CategoryId",
                table: "transactions",
                column: "CategoryId",
                principalTable: "categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UploadedFiles_categories_CategoryId",
                table: "UploadedFiles",
                column: "CategoryId",
                principalTable: "categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UploadedFiles_parameters_ParameterId",
                table: "UploadedFiles",
                column: "ParameterId",
                principalTable: "parameters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_validate_users_categories_category_id",
                table: "validate_users",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Budgets_categories_CategoryId",
                table: "Budgets");

            migrationBuilder.DropForeignKey(
                name: "FK_officeallocations_tbl_offices_office_code",
                table: "officeallocations");

            migrationBuilder.DropForeignKey(
                name: "FK_officeallocations_yearlybudgets_YearlyBudgetId",
                table: "officeallocations");

            migrationBuilder.DropForeignKey(
                name: "FK_parameters_categories_CategoryId",
                table: "parameters");

            migrationBuilder.DropForeignKey(
                name: "FK_transactions_categories_CategoryId",
                table: "transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_UploadedFiles_categories_CategoryId",
                table: "UploadedFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_UploadedFiles_parameters_ParameterId",
                table: "UploadedFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_validate_users_categories_category_id",
                table: "validate_users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_yearlybudgets",
                table: "yearlybudgets");

            migrationBuilder.DropIndex(
                name: "IX_UploadedFiles_CategoryId",
                table: "UploadedFiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_transactions",
                table: "transactions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_tbl_offices_office_code",
                table: "tbl_offices");

            migrationBuilder.DropIndex(
                name: "IX_tbl_offices_office_code",
                table: "tbl_offices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_parameters",
                table: "parameters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_categories",
                table: "categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_officeallocations",
                table: "officeallocations");

            migrationBuilder.DropIndex(
                name: "IX_officeallocations_office_code",
                table: "officeallocations");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "project_code",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "voucher_code",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "voucher_code",
                table: "tbl_transaction");

            migrationBuilder.DropColumn(
                name: "contact_person",
                table: "project_details");

            migrationBuilder.DropColumn(
                name: "office_code",
                table: "project_details");

            migrationBuilder.DropColumn(
                name: "voucher_code",
                table: "project_details");

            migrationBuilder.DropColumn(
                name: "yearly_budget_id",
                table: "project_details");

            migrationBuilder.DropColumn(
                name: "office_code",
                table: "officeallocations");

            migrationBuilder.RenameTable(
                name: "yearlybudgets",
                newName: "YearlyBudgets");

            migrationBuilder.RenameTable(
                name: "transactions",
                newName: "Transactions");

            migrationBuilder.RenameTable(
                name: "parameters",
                newName: "Parameters");

            migrationBuilder.RenameTable(
                name: "categories",
                newName: "Categories");

            migrationBuilder.RenameTable(
                name: "officeallocations",
                newName: "office_allocations");

            migrationBuilder.RenameColumn(
                name: "transaction_type",
                table: "Transactions",
                newName: "TransactionType");

            migrationBuilder.RenameIndex(
                name: "IX_transactions_CategoryId",
                table: "Transactions",
                newName: "IX_Transactions_CategoryId");

            migrationBuilder.RenameColumn(
                name: "total_budget",
                table: "project_details",
                newName: "budget");

            migrationBuilder.RenameColumn(
                name: "project",
                table: "project_details",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "create_at",
                table: "project_details",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "project_details_id",
                table: "project_details",
                newName: "status");

            migrationBuilder.RenameIndex(
                name: "IX_parameters_CategoryId",
                table: "Parameters",
                newName: "IX_Parameters_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_officeallocations_YearlyBudgetId",
                table: "office_allocations",
                newName: "IX_office_allocations_YearlyBudgetId");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Transactions",
                keyColumn: "TransactionType",
                keyValue: null,
                column: "TransactionType",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionType",
                table: "Transactions",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(45)",
                oldMaxLength: 45,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Transactions",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "office_code",
                table: "tbl_offices",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "spent_amount",
                table: "budget_allocations",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "OfficeId",
                table: "office_allocations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<decimal>(
                name: "SpentAmount",
                table: "office_allocations",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_YearlyBudgets",
                table: "YearlyBudgets",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Parameters",
                table: "Parameters",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categories",
                table: "Categories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_office_allocations",
                table: "office_allocations",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_office_allocations_OfficeId",
                table: "office_allocations",
                column: "OfficeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_Categories_CategoryId",
                table: "Budgets",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_office_allocations_YearlyBudgets_YearlyBudgetId",
                table: "office_allocations",
                column: "YearlyBudgetId",
                principalTable: "YearlyBudgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_office_allocations_tbl_offices_OfficeId",
                table: "office_allocations",
                column: "OfficeId",
                principalTable: "tbl_offices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Parameters_Categories_CategoryId",
                table: "Parameters",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Categories_CategoryId",
                table: "Transactions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_users_UserId",
                table: "Transactions",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UploadedFiles_Parameters_ParameterId",
                table: "UploadedFiles",
                column: "ParameterId",
                principalTable: "Parameters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_validate_users_Categories_category_id",
                table: "validate_users",
                column: "category_id",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
