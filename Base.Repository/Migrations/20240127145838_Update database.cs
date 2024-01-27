using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Base.Repository.Migrations
{
    public partial class Updatedatabase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TravelMode",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "AvoidFerries",
                table: "Trips",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AvoidHighways",
                table: "Trips",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AvoidTolls",
                table: "Trips",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoutingPreference",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvoidFerries",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "AvoidHighways",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "AvoidTolls",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "RoutingPreference",
                table: "Trips");

            migrationBuilder.AlterColumn<int>(
                name: "TravelMode",
                table: "Trips",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
