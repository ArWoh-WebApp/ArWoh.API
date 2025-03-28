using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArWoh.API.Migrations
{
    /// <inheritdoc />
    public partial class adssadasd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Transactions_TransactionId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Transactions_PaymentTransactionId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Images_ImageId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_CustomerId",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions");

            migrationBuilder.RenameTable(
                name: "Transactions",
                newName: "PaymentTransactions");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_ImageId",
                table: "PaymentTransactions",
                newName: "IX_PaymentTransactions_ImageId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_CustomerId",
                table: "PaymentTransactions",
                newName: "IX_PaymentTransactions_CustomerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentTransactions",
                table: "PaymentTransactions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_PaymentTransactions_TransactionId",
                table: "Orders",
                column: "TransactionId",
                principalTable: "PaymentTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PaymentTransactions_PaymentTransactionId",
                table: "Payments",
                column: "PaymentTransactionId",
                principalTable: "PaymentTransactions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_Images_ImageId",
                table: "PaymentTransactions",
                column: "ImageId",
                principalTable: "Images",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_Users_CustomerId",
                table: "PaymentTransactions",
                column: "CustomerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_PaymentTransactions_TransactionId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PaymentTransactions_PaymentTransactionId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_Images_ImageId",
                table: "PaymentTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_Users_CustomerId",
                table: "PaymentTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentTransactions",
                table: "PaymentTransactions");

            migrationBuilder.RenameTable(
                name: "PaymentTransactions",
                newName: "Transactions");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentTransactions_ImageId",
                table: "Transactions",
                newName: "IX_Transactions_ImageId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentTransactions_CustomerId",
                table: "Transactions",
                newName: "IX_Transactions_CustomerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Transactions_TransactionId",
                table: "Orders",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Transactions_PaymentTransactionId",
                table: "Payments",
                column: "PaymentTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Images_ImageId",
                table: "Transactions",
                column: "ImageId",
                principalTable: "Images",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_CustomerId",
                table: "Transactions",
                column: "CustomerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}