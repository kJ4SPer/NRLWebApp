using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBehandlingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Behandlinger");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Behandlinger",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ObstacleId = table.Column<long>(type: "bigint", nullable: false),
                    RegisterforerUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StatusId = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Comments = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProcessedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    RejectionReason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Behandlinger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Behandlinger_AspNetUsers_RegisterforerUserId",
                        column: x => x.RegisterforerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Behandlinger_ObstacleStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "ObstacleStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Behandlinger_Obstacles_ObstacleId",
                        column: x => x.ObstacleId,
                        principalTable: "Obstacles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Behandlinger_ObstacleId",
                table: "Behandlinger",
                column: "ObstacleId");

            migrationBuilder.CreateIndex(
                name: "IX_Behandlinger_RegisterforerUserId",
                table: "Behandlinger",
                column: "RegisterforerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Behandlinger_StatusId",
                table: "Behandlinger",
                column: "StatusId");
        }
    }
}
