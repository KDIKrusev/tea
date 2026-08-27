using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoyageEnergyAdvisor.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserVesselManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vessels_AspNetUsers_UserId",
                table: "Vessels");

            migrationBuilder.DropIndex(
                name: "IX_Vessels_UserId",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Vessels");

            migrationBuilder.CreateTable(
                name: "UserVessels",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VesselId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVessels", x => new { x.UserId, x.VesselId });
                    table.ForeignKey(
                        name: "FK_UserVessels_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserVessels_Vessels_VesselId",
                        column: x => x.VesselId,
                        principalTable: "Vessels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserVessels_VesselId",
                table: "UserVessels",
                column: "VesselId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserVessels");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Vessels",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Vessels_UserId",
                table: "Vessels",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vessels_AspNetUsers_UserId",
                table: "Vessels",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
