using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PUBReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddIsArchivedToBusRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    BranchID = table.Column<int>(name: "Branch_ID", type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchName = table.Column<string>(name: "Branch_Name", type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(name: "Is_Active", type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.BranchID);
                });

            migrationBuilder.CreateTable(
                name: "Bus",
                columns: table => new
                {
                    BusID = table.Column<string>(name: "Bus_ID", type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PlateNumber = table.Column<string>(name: "Plate_Number", type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BodyBusNumber = table.Column<string>(name: "Body_Bus_Number", type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BusName = table.Column<string>(name: "Bus_Name", type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BusType = table.Column<string>(name: "Bus_Type", type: "nvarchar(max)", nullable: true),
                    BusCondition = table.Column<string>(name: "Bus_Condition", type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(name: "Created_At", type: "datetime2", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bus", x => x.BusID);
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    EmployeeID = table.Column<string>(name: "Employee_ID", type: "nvarchar(15)", maxLength: 15, nullable: false),
                    FullName = table.Column<string>(name: "Full_Name", type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactNumber = table.Column<string>(name: "Contact_Number", type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    JobPosition = table.Column<string>(name: "Job_Position", type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Birthday = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HireDate = table.Column<DateTime>(name: "Hire_Date", type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(name: "Is_Active", type: "bit", nullable: false),
                    Branch = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.EmployeeID);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    RouteID = table.Column<string>(name: "Route_ID", type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsArchived = table.Column<bool>(name: "Is_Archived", type: "bit", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BaseFare = table.Column<decimal>(name: "Base_Fare", type: "decimal(9,2)", nullable: false),
                    DistanceKM = table.Column<decimal>(name: "Distance_KM", type: "decimal(7,2)", nullable: true),
                    EstimatedHours = table.Column<decimal>(name: "Estimated_Hours", type: "decimal(5,2)", nullable: true),
                    IsActive = table.Column<bool>(name: "Is_Active", type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(name: "Created_At", type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(name: "Updated_At", type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.RouteID);
                });

            migrationBuilder.CreateTable(
                name: "Trip",
                columns: table => new
                {
                    TripID = table.Column<string>(name: "Trip_ID", type: "nvarchar(12)", maxLength: 12, nullable: false),
                    BusID = table.Column<string>(name: "Bus_ID", type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EmployeeIDDriver = table.Column<string>(name: "Employee_ID_Driver", type: "nvarchar(15)", maxLength: 15, nullable: false),
                    EmployeeIDConductor = table.Column<string>(name: "Employee_ID_Conductor", type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Origin = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BaseFare = table.Column<decimal>(name: "Base_Fare", type: "decimal(9,2)", nullable: false),
                    DepartureTime = table.Column<DateTime>(name: "Departure_Time", type: "datetime2", nullable: true),
                    ActualDispatchTime = table.Column<DateTime>(name: "Actual_Dispatch_Time", type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(name: "Created_At", type: "datetime2", nullable: true),
                    ArrivalTime = table.Column<DateTime>(name: "Arrival_Time", type: "datetime2", nullable: true),
                    CancelledTime = table.Column<DateTime>(name: "Cancelled_Time", type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trip", x => x.TripID);
                    table.ForeignKey(
                        name: "FK_Trip_Bus_Bus_ID",
                        column: x => x.BusID,
                        principalTable: "Bus",
                        principalColumn: "Bus_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Trip_Employee_Employee_ID_Conductor",
                        column: x => x.EmployeeIDConductor,
                        principalTable: "Employee",
                        principalColumn: "Employee_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Trip_Employee_Employee_ID_Driver",
                        column: x => x.EmployeeIDDriver,
                        principalTable: "Employee",
                        principalColumn: "Employee_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(name: "User_ID", type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeID = table.Column<string>(name: "Employee_ID", type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(name: "Password_Hash", type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(name: "Is_Active", type: "bit", nullable: false),
                    LastLogin = table.Column<DateTime>(name: "Last_Login", type: "datetime2", nullable: true),
                    LoginAttempts = table.Column<int>(name: "Login_Attempts", type: "int", nullable: false),
                    AccountLocked = table.Column<bool>(name: "Account_Locked", type: "bit", nullable: false),
                    LockedAt = table.Column<DateTime>(name: "Locked_At", type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(name: "Created_At", type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_Users_Employee_Employee_ID",
                        column: x => x.EmployeeID,
                        principalTable: "Employee",
                        principalColumn: "Employee_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Audit_Log",
                columns: table => new
                {
                    LogID = table.Column<int>(name: "Log_ID", type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(name: "User_ID", type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Terminal = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audit_Log", x => x.LogID);
                    table.ForeignKey(
                        name: "FK_Audit_Log_Users_User_ID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "User_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservation",
                columns: table => new
                {
                    ReservationID = table.Column<string>(name: "Reservation_ID", type: "nvarchar(15)", maxLength: 15, nullable: false),
                    TripID = table.Column<string>(name: "Trip_ID", type: "nvarchar(12)", maxLength: 12, nullable: false),
                    UserID = table.Column<int>(name: "User_ID", type: "int", nullable: false),
                    GroupID = table.Column<string>(name: "Group_ID", type: "nvarchar(15)", maxLength: 15, nullable: true),
                    IsGroupBooking = table.Column<bool>(name: "Is_Group_Booking", type: "bit", nullable: false),
                    ContactPerson = table.Column<string>(name: "Contact_Person", type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PassengerName = table.Column<string>(name: "Passenger_Name", type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactNumber = table.Column<string>(name: "Contact_Number", type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PassengerType = table.Column<string>(name: "Passenger_Type", type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DiscountPercentage = table.Column<decimal>(name: "Discount_Percentage", type: "decimal(5,2)", nullable: false),
                    IDNumber = table.Column<string>(name: "ID_Number", type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReservationDate = table.Column<DateTime>(name: "Reservation_Date", type: "datetime2", nullable: false),
                    SeatNumber = table.Column<int>(name: "Seat_Number", type: "int", nullable: false),
                    NumberofSeats = table.Column<int>(name: "Number_of_Seats", type: "int", nullable: false),
                    BaseFare = table.Column<decimal>(name: "Base_Fare", type: "decimal(9,2)", nullable: false),
                    DiscountApplied = table.Column<decimal>(name: "Discount_Applied", type: "decimal(9,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(name: "Total_Amount", type: "decimal(9,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    CreatedAt = table.Column<DateTime>(name: "Created_At", type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservation", x => x.ReservationID);
                    table.ForeignKey(
                        name: "FK_Reservation_Trip_Trip_ID",
                        column: x => x.TripID,
                        principalTable: "Trip",
                        principalColumn: "Trip_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Users_User_ID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "User_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    PaymentID = table.Column<string>(name: "Payment_ID", type: "nvarchar(15)", maxLength: 15, nullable: false),
                    ReservationID = table.Column<string>(name: "Reservation_ID", type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(9,2)", nullable: false),
                    PaymentMethod = table.Column<string>(name: "Payment_Method", type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PaymentStatus = table.Column<string>(name: "Payment_Status", type: "nvarchar(15)", maxLength: 15, nullable: false),
                    ReferenceNumber = table.Column<string>(name: "Reference_Number", type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PaymentDate = table.Column<DateTime>(name: "Payment_Date", type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.PaymentID);
                    table.ForeignKey(
                        name: "FK_Payment_Reservation_Reservation_ID",
                        column: x => x.ReservationID,
                        principalTable: "Reservation",
                        principalColumn: "Reservation_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Audit_Log_User_ID",
                table: "Audit_Log",
                column: "User_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_Reservation_ID",
                table: "Payment",
                column: "Reservation_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_Trip_ID",
                table: "Reservation",
                column: "Trip_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_User_ID",
                table: "Reservation",
                column: "User_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Trip_Bus_ID",
                table: "Trip",
                column: "Bus_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Trip_Employee_ID_Conductor",
                table: "Trip",
                column: "Employee_ID_Conductor");

            migrationBuilder.CreateIndex(
                name: "IX_Trip_Employee_ID_Driver",
                table: "Trip",
                column: "Employee_ID_Driver");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Employee_ID",
                table: "Users",
                column: "Employee_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Audit_Log");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "Reservation");

            migrationBuilder.DropTable(
                name: "Trip");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Bus");

            migrationBuilder.DropTable(
                name: "Employee");
        }
    }
}
