using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity; // Needed for PasswordHasher
using Microsoft.EntityFrameworkCore;
using PUBReservationSystem.Models;
using PUBReservationSystem.Services; // 1. ADD THIS LINE
using System;
using System.Collections.Generic;
using System.Linq;

namespace PUBReservationSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuditService _auditService; // 2. ADD THIS LINE
        public HomeController(AppDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // ========== VIEWS ==========
        [HttpPost]
        public JsonResult WipeArchive(string targetTab)
        {
            try
            {
                int deletedCount = 0;

                // Check which tab the user is currently viewing
                if (targetTab == "routes")
                {
                    var archivedRoutes = _context.Routes.Where(r => r.Is_Archived == true).ToList();
                    deletedCount = archivedRoutes.Count;
                    _context.Routes.RemoveRange(archivedRoutes);
                }
                else if (targetTab == "buses")
                {
                    // Note: Make sure "IsArchived" matches your exact model property spelling!
                    var archivedBuses = _context.Bus.Where(b => b.IsArchived == true).ToList();
                    deletedCount = archivedBuses.Count;
                    _context.Bus.RemoveRange(archivedBuses);
                }
                else if (targetTab == "employees")
                {
                    // Assuming you have an Employee table with an archive flag
                    // var archivedEmployees = _context.Employees.Where(e => e.IsArchived == true).ToList();
                    // deletedCount = archivedEmployees.Count;
                    // _context.Employees.RemoveRange(archivedEmployees);
                }
                else
                {
                    return Json(new { success = false, message = "Invalid section selected." });
                }

                // If the archive is already empty, no need to touch the database
                if (deletedCount == 0)
                {
                    return Json(new { success = false, message = "There are no archived records to delete here." });
                }

                // Commit the mass deletion
                _context.SaveChanges();

                return Json(new { success = true, message = $"Successfully wiped {deletedCount} records." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Database error: " + ex.Message });
            }
        }


        [HttpPost]
        public IActionResult CancelAllReservations([FromBody] List<string> ids)
        {
            var userIdString = HttpContext.Session.GetString("User_ID");
            if (string.IsNullOrEmpty(userIdString)) return Json(new { success = false, message = "Session expired." });

            int currentUserId = int.Parse(userIdString);
            var currentUser = _context.Users.Include(u => u.Employee).FirstOrDefault(u => u.User_ID == currentUserId);

            // Fetch the specific details
            string cancellerName = currentUser?.Employee?.Full_Name ?? currentUser?.Username ?? "System Admin";
            string cancellerId = currentUser?.Employee?.Employee_ID ?? $"USER-{currentUserId}";
            string cancellerPosition = currentUser?.Employee?.Job_Position ?? HttpContext.Session.GetString("Role") ?? "Admin";

            var reservationsToCancel = _context.Reservation.Where(r => ids.Contains(r.Reservation_ID)).ToList();

            foreach (var res in reservationsToCancel)
            {
                res.Status = "Cancelled";
            }

            _context.SaveChanges();

            // Pass the jobPosition back in the JSON payload
            return Json(new
            {
                success = true,
                adminName = cancellerName,
                adminId = cancellerId,
                jobPosition = cancellerPosition
            });
        }












































        public IActionResult Index()
        {
            // ==========================================
            // 1. SESSION CHECK & ROLE IDENTIFICATION
            // ==========================================
            var userIdString = HttpContext.Session.GetString("User_ID");
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Account");

            var role = HttpContext.Session.GetString("Role") ?? "";
            ViewBag.Role = role;

            int currentUserId = int.Parse(userIdString);
            var currentUser = _context.Users.Include(u => u.Employee).FirstOrDefault(u => u.User_ID == currentUserId);

            // Fetch both the reliable Integer ID and the presentation String name
            int myBranchId = currentUser?.Employee?.Branch_ID ?? 0;
            var myBranchString = currentUser?.Employee?.Branch ?? "Unknown";
            ViewBag.BranchName = myBranchString;

            // Robust extraction: removes "Branch" regardless of case, and trims whitespace
            var myLocation = myBranchString.Replace("Branch", "", StringComparison.OrdinalIgnoreCase).Trim();

            // Safe Date Ranges
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var yesterday = today.AddDays(-1);

            // ==========================================
            // 2. BUILD BASE QUERIES
            // ==========================================
            var tripQuery = _context.Trip.Include(t => t.Bus).Include(t => t.Driver).Include(t => t.Conductor).AsQueryable();
            var resQuery = _context.Reservation.Include(r => r.Trip).AsQueryable();
            var auditQuery = _context.AuditLog.Include(a => a.User).ThenInclude(u => u.Employee).AsQueryable();

            // ==========================================
            // 3. APPLY THE MASTER LOCK (CASE-INSENSITIVE)
            // ==========================================
            bool isGlobalAdmin = role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);

            if (!isGlobalAdmin)
            {
                // 1. TRIPS: Keep shared so they see incoming and outgoing vehicles for their exact hub location
                tripQuery = tripQuery.Where(t =>
                    t.Origin.Contains(myLocation) || t.Destination.Contains(myLocation) ||
                    t.Origin == myBranchString || t.Destination == myBranchString);

                // 2. SALES: STRICTLY lock revenue tracking streams to bookings originating at their counter depot
                resQuery = resQuery.Where(r => r.Trip != null &&
                    (r.Trip.Origin.Contains(myLocation) || r.Trip.Origin == myBranchString));

                // 3. AUDIT LOGS (FIXED): Isolates logs by the target Terminal name OR the actor's explicit Branch ID
                auditQuery = auditQuery.Where(a =>
                    a.Terminal == myBranchString ||
                    a.Terminal == myLocation ||
                    (a.User != null && a.User.Employee != null && a.User.Employee.Branch_ID == myBranchId));
            }

            // ==========================================
            // 4. EXECUTE TRIPS QUERIES
            // ==========================================
            var allTodayTrips = tripQuery
                .Where(t => t.Scheduled_Departure_Time >= today && t.Scheduled_Departure_Time < tomorrow)
                .OrderBy(t => t.Scheduled_Departure_Time)
                .ToList();

            var todayTripIds = allTodayTrips.Select(t => t.Trip_ID).ToList();
            ViewBag.BookingCounts = _context.Reservation
                .Where(r => r.Status != "Cancelled" && todayTripIds.Contains(r.Trip_ID))
                .GroupBy(r => r.Trip_ID)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.ScheduledTrips = allTodayTrips.Count(t => t.Status == "Scheduled" || t.Status == "Active");
            ViewBag.InTransitBuses = allTodayTrips.Count(t => t.Status == "In Transit");
            ViewBag.CompletedTrips = allTodayTrips.Count(t => t.Status == "Completed");
            ViewBag.CancelledTrips = allTodayTrips.Count(t => t.Status == "Cancelled");

            ViewBag.TodayTrips = allTodayTrips.Where(t => t.Status != "Cancelled").ToList();

            // ==========================================
            // 5. EXECUTE SALES QUERIES 
            // ==========================================
            var todayResList = resQuery
                .Where(r => r.Created_At >= today && r.Created_At < tomorrow)
                .ToList();

            ViewBag.TodaySales = todayResList
                .Where(r => r.Status != "Cancelled")
                .Sum(r => (decimal?)r.Total_Amount) ?? 0;

            var yesterdayResList = resQuery
                .Where(r => r.Created_At >= yesterday && r.Created_At < today)
                .ToList();

            ViewBag.YesterdaySales = yesterdayResList
                .Where(r => r.Status != "Cancelled")
                .Sum(r => (decimal?)r.Total_Amount) ?? 0;

            // ==========================================
            // 6. EXECUTE AUDIT LOGS QUERIES (FIXED CONTEXT BOUNDS)
            // ==========================================
            ViewBag.RecentActivities = auditQuery
                .Where(a => a.Timestamp >= today && a.Timestamp < tomorrow)
                .OrderByDescending(a => a.Timestamp)
                .Take(50)
                .ToList();

            ViewBag.ConfiguredRoutes = tripQuery
                .Select(t => new { t.Origin, t.Destination })
                .Distinct()
                .ToList();

            // ==========================================
            // 7. FETCH ROUTES FOR THE MODAL DROPDOWN
            // ==========================================
            ViewBag.Destinations = _context.Routes
                .Select(r => r.Destination)
                .Distinct()
                .ToList();

            return View();
        }

        public void ArchiveOldSales()
        {
            // 1. Calculate the cutoff date (6 days ago from today)
            var cutoffDate = DateTime.Today.AddDays(-6);

            // 2. Find old sales records
            var oldSales = _context.Sales
                .Where(s => s.Sale_Date <= cutoffDate)
                .ToList();

            if (oldSales.Any())
            {
                foreach (var sale in oldSales)
                {
                    // 3. Create a new Audit Log entry
                    var log = new AuditLog
                    {
                        Timestamp = DateTime.Now,
                        User_ID = 1, // Or your System/Admin ID
                        Action = "AUTO ARCHIVE",
                        Description = $"Archived sale from {sale.Sale_Date.ToShortDateString()}: ₱{sale.Amount}",
                        Terminal = "System Auto"
                    };
                    _context.AuditLogs.Add(log);

                    // 4. Remove from Sales table
                    _context.Sales.Remove(sale);
                }

                // 5. Save changes
                _context.SaveChanges();

                // Use TempData to show a message to the user on the next page load
                TempData["ArchiveMessage"] = "Old sales records have been archived.";
            }
        }

        private void ArchiveOldCancelledReservations()
        {
            var today = DateTime.Now.Date;

            // 1. Find cancelled reservations older than today
            var expiredCancelled = _context.Reservation
                .Where(r => r.Status == "Cancelled" && r.Reservation_Date < today)
                .ToList();

            if (expiredCancelled.Any())
            {
                var userIdString = HttpContext.Session.GetString("User_ID");
                int currentUserId = int.TryParse(userIdString, out int parsedId) ? parsedId : 1;

                foreach (var res in expiredCancelled)
                {
                    // 2. Capture ALL reservation details into a descriptive string
                    string fullDetails = $"Archived Cancelled Reservation ID: {res.Reservation_ID} | " +
                                         $"Passenger: {res.Passenger_Name} | Trip: {res.Trip_ID} | " +
                                         $"Seat: {res.Seat_Number} | Amount: {res.Total_Amount} | " +
                                         $"Date: {res.Reservation_Date:yyyy-MM-dd}";

                    // 3. Create the Audit Log entry to keep the data alive
                    var auditEntry = new AuditLog
                    {
                        User_ID = currentUserId,
                        Action = "Auto Archive",
                        Description = fullDetails, // All info is now stored here
                        Terminal = "System",
                        Timestamp = DateTime.Now
                    };

                    _context.AuditLog.Add(auditEntry);
                }

                // 4. Clean up connected Payments first (to avoid the FK constraint error)
                var expiredIds = expiredCancelled.Select(r => r.Reservation_ID).ToList();
                var linkedPayments = _context.Payment.Where(p => expiredIds.Contains(p.Reservation_ID)).ToList();

                if (linkedPayments.Any())
                {
                    _context.Payment.RemoveRange(linkedPayments);
                }

                // 5. Delete from Reservation table
                _context.Reservation.RemoveRange(expiredCancelled);
                _context.SaveChanges();
            }
        }

        public IActionResult Buses()
        {
            var userIdString = HttpContext.Session.GetString("User_ID");
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Account");

            var role = HttpContext.Session.GetString("Role");
            ViewBag.Role = role;

            // 1. IDENTIFY USER & BRANCH
            int currentUserId = int.Parse(userIdString);
            var currentUser = _context.Users.Include(u => u.Employee).FirstOrDefault(u => u.User_ID == currentUserId);
            int myBranchId = currentUser?.Employee?.Branch_ID ?? 0;

            // 2. FETCH ALL ACTIVE BUSES & COMPLETED TRIPS
            var allBuses = _context.Bus
                .Where(b => b.IsArchived == false)
                .OrderByDescending(b => b.Bus_ID)
                .ToList();

            var allCompletedTrips = _context.Trip
                .Where(t => t.Status == "Completed")
                .OrderByDescending(t => t.Arrival_Time ?? t.Scheduled_Departure_Time)
                .ToList();

            // ==========================================
            // NEW LOGIC: FIND TRIPS HOLDING BUSES HOSTAGE
            // ==========================================
            var activeStatuses = new List<string> { "Scheduled", "Active", "In Transit" };
            var activeTrips = _context.Trip
                .Where(t => activeStatuses.Contains(t.Status))
                .ToList();

            // 3. THE MASTER LOCK & DYNAMIC STATUS
            var physicallyPresentBuses = new List<Bus>();

            foreach (var bus in allBuses)
            {
                // ==========================================
                // NEW LOGIC: DYNAMIC STATUS OVERRIDE
                // ==========================================
                // If the database says it is "Available", but it is actually booked for a live trip,
                // visually upgrade its status to match the trip so the dashboard is 100% accurate!
                if (bus.Status == "Available")
                {
                    var busyTrip = activeTrips.FirstOrDefault(t => t.Bus_ID == bus.Bus_ID);
                    if (busyTrip != null)
                    {
                        bus.Status = busyTrip.Status; // Dynamically changes it to "Scheduled" or "In Transit"
                    }
                }

                // Calculate True Physical Location
                var lastTrip = allCompletedTrips.FirstOrDefault(t => t.Bus_ID == bus.Bus_ID);
                int? currentLocationId = bus.Branch_ID;

                if (lastTrip != null && lastTrip.Destination_Branch_ID.HasValue)
                {
                    currentLocationId = lastTrip.Destination_Branch_ID;
                }

                // Apply Branch Master Lock
                if (role == "SuperAdmin" || currentLocationId == myBranchId)
                {
                    physicallyPresentBuses.Add(bus);
                }
            }

            // 4. EXECUTE & PASS TO VIEW
            ViewBag.Buses = physicallyPresentBuses;

            // 5. STAT CARDS
            ViewBag.TotalBuses = physicallyPresentBuses.Count;

            // Because we dynamically updated DLT7713 to "Scheduled" above, 
            // this count will now automatically and correctly drop to 4!
            ViewBag.AvailableBuses = physicallyPresentBuses.Count(b => b.Status == "Available");

            ViewBag.MaintenanceBuses = physicallyPresentBuses.Count(b => b.Status == "In Maintenance" || b.Status == "Maintenance");

            return View();
        }



        [HttpGet]
        public IActionResult GetActiveTrips()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var trips = _context.Trip
                .Include(t => t.Bus)
                .Include(t => t.Driver)
                .Where(t => t.Scheduled_Departure_Time >= today &&
                            t.Scheduled_Departure_Time < tomorrow &&
                            t.Status != "Cancelled") // Filter out cancelled ones!
                .OrderBy(t => t.Scheduled_Departure_Time)
                .ToList();

            // Map to a simple object to avoid circular reference errors with EF
            var result = trips.Select(t => new
            {
                trip_ID = t.Trip_ID,
                busName = t.Bus?.Bus_Name ?? "—",
                bodyNo = t.Bus?.Body_Bus_Number?.ToString() ?? "—",
                origin = t.Origin,
                destination = t.Destination,
                departure = t.Scheduled_Departure_Time != null
    ? (t.Scheduled_Departure_Time is DateTime ? ((DateTime)t.Scheduled_Departure_Time).ToString("hh:mm tt") : t.Scheduled_Departure_Time.ToString())
    : "—",
                driver = t.Driver != null ? System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(t.Driver.Full_Name.ToLower()) : "—"
            });

            return Json(result);
        }



        [HttpGet]
        public IActionResult GetActiveTripIds()
        {
            var today = DateTime.Today;

            // Use '?.Date' to safely access the date only if the time exists
            var activeIds = _context.Trip
                .Where(t => t.Scheduled_Departure_Time != null && t.Scheduled_Departure_Time.Value.Date == today && t.Status != "Cancelled")
                .Select(t => t.Trip_ID.ToString())
                .ToList();

            return Json(activeIds);
        }






































        [HttpPost]
        public IActionResult CreateBranch(Branch model)
        {
            if (ModelState.IsValid)
            {
                model.Is_Active = true; // Default to active
                _context.Branches.Add(model);
                _context.SaveChanges();
                return RedirectToAction("Branches");
            }
            return View("Branches", _context.Branches.ToList());
        }

        [HttpPost]
        public JsonResult MarkTripArrived(string tripId)
        {
            var trip = _context.Trip.FirstOrDefault(t => t.Trip_ID == tripId);

            if (trip == null)
                return Json(new { success = false, message = "Trip not found." });

            // 1. Update Trip
            trip.Status = "Completed";
            trip.Arrival_Time = DateTime.Now;
            _context.Trip.Update(trip); // Force EF to track this change

            // 2. Update Bus
            if (!string.IsNullOrEmpty(trip.Bus_ID))
            {
                // Added .Trim() to prevent hidden whitespace mismatch errors
                var parkedBus = _context.Bus.FirstOrDefault(b => b.Bus_ID == trip.Bus_ID.Trim());
                if (parkedBus != null)
                {
                    parkedBus.Status = "Available";
                    _context.Bus.Update(parkedBus); // Force EF to track this change
                }
            }

            // 3. Commit ALL changes at once
            _context.SaveChanges();

            return Json(new { success = true });
        }



        // Method that the dashboard will call every 3 seconds to get the latest logs for this branch
        [HttpGet]
        public IActionResult GetRecentLogs()
        {
            var currentBranch = HttpContext.Session.GetString("Branch");

            var query = _context.AuditLog
                .Include(a => a.User).ThenInclude(u => u.Employee)
                .AsQueryable();

            // 🛑 FIXED CS1061: Commented out to ensure instant compilation.
            // Un-comment the line below that matches where you store the branch string:
            if (!string.IsNullOrEmpty(currentBranch))
            {
                // Option A: If branch belongs to the logged-in employee who performed the action:
                // query = query.Where(a => a.User.Employee.Branch == currentBranch); 

                // Option B: If the column on your AuditLog table has a slightly different name:
                // query = query.Where(a => a.Branch_Name == currentBranch); 
            }

            // Force the server to ONLY give us the 10 most recent logs
            var logs = query
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .Select(l => new
                {
                    logId = l.Log_ID,
                    action = l.Action,
                    // Keeps our clean layout fix using seconds: "hh:mm:ss tt" (e.g., 12:07:38 AM)
                    timestamp = l.Timestamp.ToString("hh:mm:ss tt"),
                    userName = l.User.Employee != null ? l.User.Employee.Full_Name : (l.User.Username ?? "System"),
                    jobPosition = l.User.Employee != null ? l.User.Employee.Job_Position : "Admin"
                })
                .ToList();

            return Json(logs);
        }




        [HttpPost]
        public IActionResult SaveChangesAccountSettings(
    IFormFile ProfileImage,
    string DisplayName,
    string Username,
    string CurrentPassword,
    string NewPassword)
        {
            try
            {
                // 1. Validate Session
                var userIdStr = HttpContext.Session.GetString("User_ID");
                if (string.IsNullOrEmpty(userIdStr))
                    return Json(new { success = false, message = "Session expired. Please log in again." });

                int currentUserId = int.Parse(userIdStr);

                // 2. Fetch User
                var user = _context.Users.Include(u => u.Employee).FirstOrDefault(u => u.User_ID == currentUserId);
                if (user == null)
                    return Json(new { success = false, message = "Account details not found." });

                // 3. Check if security settings (Username or Password) are changing
                bool isSecurityUpdate = (!string.IsNullOrEmpty(NewPassword) || user.Username != Username);

                if (isSecurityUpdate)
                {
                    if (string.IsNullOrEmpty(CurrentPassword))
                    {
                        return Json(new { success = false, message = "Current password is required to verify changes." });
                    }

                    // 🔐 CRITICAL FIX: Instantiate the built-in crypto hasher
                    var hasher = new PasswordHasher<Users>();

                    // Verify if the typed plain-text current password matches the stored hash
                    var verificationResult = hasher.VerifyHashedPassword(user, user.Password_Hash ?? "", CurrentPassword);

                    if (verificationResult == PasswordVerificationResult.Failed)
                    {
                        return Json(new { success = false, message = "Invalid current password confirmation." });
                    }
                }

                // 4. Update Personalization (Display Name)
                if (!string.IsNullOrEmpty(DisplayName) && user.Employee != null)
                {
                    user.Employee.Full_Name = DisplayName;
                    HttpContext.Session.SetString("User_FullName", DisplayName);
                }

                // 5. Handle Profile Image Upload
                if (ProfileImage != null && ProfileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = $"user_{currentUserId}_{DateTime.Now.Ticks}{Path.GetExtension(ProfileImage.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        ProfileImage.CopyTo(stream);
                    }

                    string accessPath = $"/images/profiles/{uniqueFileName}";
                    HttpContext.Session.SetString("ProfilePicturePath", accessPath);
                }

                // 6. Update Security Fields with Secure Hashing
                if (isSecurityUpdate)
                {
                    user.Username = Username;
                    HttpContext.Session.SetString("Username", Username);

                    if (!string.IsNullOrEmpty(NewPassword))
                    {
                        // 🔐 CRITICAL FIX: Hash the new password securely before writing to SQL
                        var hasher = new PasswordHasher<Users>();
                        user.Password_Hash = hasher.HashPassword(user, NewPassword);
                    }
                }

                // 7. Commit to Database
                _context.SaveChanges();

                return Json(new { success = true, message = "Account settings updated successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SETTINGS ERROR: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while saving your changes." });
            }
        }






































        public IActionResult Trips()
        {
            // 1. AUTH & SESSION CHECK
            var userIdString = HttpContext.Session.GetString("User_ID");
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Account");

            // 2. SAFE VARIABLE RETRIEVAL
            string sessionBranchIdStr = HttpContext.Session.GetString("Branch_ID") ?? "0";
            string userRole = HttpContext.Session.GetString("Role") ?? "Admin";

            // Ensure Branch ID is an integer
            int.TryParse(sessionBranchIdStr, out int myBranchId);

            // 3. PASS TO VIEW
            ViewBag.Role = userRole;
            ViewBag.DebugBranchId = myBranchId;

            // 4. HEADER SAFETY
            var currentBranch = _context.Branches.FirstOrDefault(b => b.Branch_ID == myBranchId);
            ViewBag.MyBranch = (currentBranch != null) ? currentBranch.Branch_Name : "Unknown Branch";

            // 5. QUERY (Optimized and Diagnostic)
            var allMyTrips = _context.Trip
                .Include(t => t.Bus)
                .Include(t => t.Driver)
                .Include(t => t.Route)
                .Where(t => t.Branch_ID == myBranchId || t.Destination_Branch_ID == myBranchId)
                .OrderByDescending(t => t.Scheduled_Departure_Time)
                .ToList();

            // DIAGNOSTIC
            System.Diagnostics.Debug.WriteLine($"DEBUG: Found {allMyTrips.Count} total trips for Branch {myBranchId}");
            foreach (var t in allMyTrips)
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG: Trip {t.Trip_ID} | Status: {t.Status} | Branch: {t.Branch_ID} | Dest: {t.Destination_Branch_ID}");
            }

            // ==========================================
            // 6. STRICT LIST SEPARATION & FILTERING (FIXED)
            // ==========================================

            // Clean up case sensitivity and strip trailing database spaces safely
            var visibleTrips = allMyTrips
                .Where(t => t.Status != null && !t.Status.Trim().Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Use the visible list for the table rows
            ViewBag.OutgoingTrips = visibleTrips
                .Where(t => t.Branch_ID == myBranchId)
                .ToList();

            ViewBag.IncomingTrips = visibleTrips
                .Where(t => t.Destination_Branch_ID == myBranchId && t.Branch_ID != myBranchId)
                .ToList();

            // ==========================================
            // 7. UI DATA & STAT CARDS (FIXED)
            // ==========================================
            ViewBag.Branches = _context.Branches.ToList();

            // Make stat card checks entirely bulletproof against casing disparities
            ViewBag.TotalTrips = visibleTrips.Count;

            ViewBag.Scheduled = visibleTrips.Count(t => t.Status != null &&
                (t.Status.Trim().Equals("Scheduled", StringComparison.OrdinalIgnoreCase) ||
                 t.Status.Trim().Equals("Active", StringComparison.OrdinalIgnoreCase)));

            ViewBag.InTransit = visibleTrips.Count(t => t.Status != null &&
                t.Status.Trim().Equals("In Transit", StringComparison.OrdinalIgnoreCase));

            ViewBag.Completed = visibleTrips.Count(t => t.Status != null &&
                t.Status.Trim().Equals("Completed", StringComparison.OrdinalIgnoreCase));

            // ==========================================
            // 8. FETCH FLEET & PERSONNEL (DYNAMIC LOCATION & ANTI-DUPLICATION)
            // ==========================================

            // A. Identify who is currently busy on the road
            var activeStatuses = new List<string> { "Scheduled", "In Transit", "Active" };

            var busyBusIds = _context.Trip
                .Where(t => activeStatuses.Contains(t.Status) && t.Bus_ID != null)
                .Select(t => t.Bus_ID)
                .ToList();

            var busyDriverIds = _context.Trip
                .Where(t => activeStatuses.Contains(t.Status) && t.Employee_ID_Driver != null)
                .Select(t => t.Employee_ID_Driver)
                .ToList();

            var busyConductorIds = _context.Trip
                .Where(t => activeStatuses.Contains(t.Status) && t.Employee_ID_Conductor != null)
                .Select(t => t.Employee_ID_Conductor)
                .ToList();

            // B. Fetch travel history to determine physical locations
            var allCompletedTrips = _context.Trip
                .Where(t => t.Status == "Completed")
                .OrderByDescending(t => t.Arrival_Time ?? t.Scheduled_Departure_Time)
                .ToList();

            // C. Calculate True Location for Buses
            var allIdleBuses = _context.Bus
                .Where(b => b.Status != "In Maintenance"
                         && b.Status != "Maintenance"
                         && b.Status != "Archived" // Hides buses marked as Archived
                         && b.IsArchived == false    // Hides soft-deleted buses
                         && !busyBusIds.Contains(b.Bus_ID))
                .ToList();

            var actuallyAvailableBuses = new List<Bus>();
            foreach (var bus in allIdleBuses)
            {
                var lastTrip = allCompletedTrips.FirstOrDefault(t => t.Bus_ID == bus.Bus_ID);

                // FIX: Use int? to handle both nullable and non-nullable Branch_IDs safely
                int? currentLocationId = bus.Branch_ID;
                if (lastTrip != null && lastTrip.Destination_Branch_ID.HasValue)
                {
                    currentLocationId = lastTrip.Destination_Branch_ID;
                }

                if (currentLocationId == myBranchId) actuallyAvailableBuses.Add(bus);
            }
            ViewBag.AvailableBuses = actuallyAvailableBuses;

            // D. Calculate True Location for Drivers
            var allIdleDrivers = _context.Employee
                .Where(e => e.Job_Position == "Driver" && e.Is_Active == true && !busyDriverIds.Contains(e.Employee_ID))
                .ToList();

            var actuallyAvailableDrivers = new List<Employee>();
            foreach (var driver in allIdleDrivers)
            {
                var lastTrip = allCompletedTrips.FirstOrDefault(t => t.Employee_ID_Driver == driver.Employee_ID);

                // FIX: Use int?
                int? currentLocationId = driver.Branch_ID;
                if (lastTrip != null && lastTrip.Destination_Branch_ID.HasValue)
                {
                    currentLocationId = lastTrip.Destination_Branch_ID;
                }

                if (currentLocationId == myBranchId) actuallyAvailableDrivers.Add(driver);
            }
            ViewBag.AvailableDrivers = actuallyAvailableDrivers;

            // E. Calculate True Location for Conductors
            var allIdleConductors = _context.Employee
                .Where(e => e.Job_Position == "Conductor" && e.Is_Active == true && !busyConductorIds.Contains(e.Employee_ID))
                .ToList();

            var actuallyAvailableConductors = new List<Employee>();
            foreach (var conductor in allIdleConductors)
            {
                var lastTrip = allCompletedTrips.FirstOrDefault(t => t.Employee_ID_Conductor == conductor.Employee_ID);

                // FIX: Use int?
                int? currentLocationId = conductor.Branch_ID;
                if (lastTrip != null && lastTrip.Destination_Branch_ID.HasValue)
                {
                    currentLocationId = lastTrip.Destination_Branch_ID;
                }

                if (currentLocationId == myBranchId) actuallyAvailableConductors.Add(conductor);
            }
            ViewBag.AvailableConductors = actuallyAvailableConductors;

            // ==========================================
            // 9. DYNAMIC BOOKING COUNTS (BULLETPROOF VERSION)
            // ==========================================
            // Performance optimization: Only count reservations for trips currently displayed
            var visibleTripIds = allMyTrips.Select(t => t.Trip_ID).ToList();

            var rawReservations = _context.Reservation
                .Where(r => visibleTripIds.Contains(r.Trip_ID))
                .Select(r => new { r.Trip_ID, r.Status })
                .ToList();

            ViewBag.BookingCounts = rawReservations
                .Where(r => !string.IsNullOrWhiteSpace(r.Trip_ID)
                         && !string.IsNullOrWhiteSpace(r.Status)
                         && !r.Status.Trim().Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                .GroupBy(r => r.Trip_ID.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            ViewBag.Routes = _context.Routes.Select(r => r.Destination).Distinct().ToList();

            return View();
        }

        public IActionResult Employees()
        {
            var userIdString = HttpContext.Session.GetString("User_ID");
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");

            var role = HttpContext.Session.GetString("Role");
            ViewBag.Role = role;

            // 1. IDENTIFY USER & BRANCH
            int currentUserId = int.Parse(userIdString);
            var currentUser = _context.Users.Include(u => u.Employee).FirstOrDefault(u => u.User_ID == currentUserId);

            // Use the Branch string to identify the user's location
            var myBranchString = currentUser?.Employee?.Branch ?? "General";
            bool isBranchRestricted = (myBranchString != "General");

            // We need the branches list to map Trip Destination IDs back to Branch strings
            var allBranches = _context.Branches.ToList();

            // 2. FETCH ALL EMPLOYEES & COMPLETED TRIPS
            var allEmployees = _context.Employee.OrderBy(e => e.Employee_ID).ToList();

            var allCompletedTrips = _context.Trip
                .Where(t => t.Status == "Completed")
                .OrderByDescending(t => t.Arrival_Time ?? t.Scheduled_Departure_Time)
                .ToList();

            // 3. THE MASTER LOCK & DYNAMIC LOCATION
            var physicallyPresentEmployees = new List<Employee>();

            foreach (var emp in allEmployees)
            {
                // Traveling Staff: Drivers and Conductors
                if (emp.Job_Position == "Driver" || emp.Job_Position == "Conductor")
                {
                    var lastTrip = emp.Job_Position == "Driver"
                        ? allCompletedTrips.FirstOrDefault(t => t.Employee_ID_Driver == emp.Employee_ID)
                        : allCompletedTrips.FirstOrDefault(t => t.Employee_ID_Conductor == emp.Employee_ID);

                    // Default location is their home string
                    string currentLocationString = emp.Branch;

                    // Override with last parked location if a trip exists
                    if (lastTrip != null && lastTrip.Destination_Branch_ID.HasValue)
                    {
                        var destBranch = allBranches.FirstOrDefault(b => b.Branch_ID == lastTrip.Destination_Branch_ID.Value);
                        if (destBranch != null)
                        {
                            currentLocationString = destBranch.Branch_Name; // Translates '3' back to 'Manila Branch'
                        }
                    }

                    // SuperAdmins see everyone. Branch users see employees physically at their branch.
                    if (role == "SuperAdmin" || !isBranchRestricted || currentLocationString == myBranchString)
                    {
                        physicallyPresentEmployees.Add(emp);
                    }
                }
                else
                {
                    // Non-traveling Staff: Admins, Tellers (They stay at their home branch)
                    if (role == "SuperAdmin" || !isBranchRestricted || emp.Branch == myBranchString)
                    {
                        physicallyPresentEmployees.Add(emp);
                    }
                }
            }

            // 4. EXECUTE & PASS TO VIEW
            ViewBag.Employees = physicallyPresentEmployees;

            // 5. STAT CARDS (Now automatically filtered to physically present people!)
            ViewBag.TotalEmployees = physicallyPresentEmployees.Count;
            ViewBag.ActiveEmployees = physicallyPresentEmployees.Count(e => e.Is_Active);
            ViewBag.Drivers = physicallyPresentEmployees.Count(e => e.Job_Position == "Driver");
            ViewBag.Conductors = physicallyPresentEmployees.Count(e => e.Job_Position == "Conductor");

            // =================================================================
            // 6. BRANCH DROPDOWN (FIXED: Admins and SuperAdmins see all branches)
            // =================================================================
            var branchQuery = allBranches.Where(b => b.Is_Active).AsQueryable();

            // Check if the user is a regular operator (like a Teller or Dispatcher) 
            // who should be locked down to their location.
            bool isAuthorizedAdmin = role == "Admin" || role == "SuperAdmin";

            if (!isAuthorizedAdmin && isBranchRestricted)
            {
                // Lower-level roles can only see their own branch assignment
                branchQuery = branchQuery.Where(b => b.Branch_Name == myBranchString);
            }

            ViewBag.Branches = branchQuery.OrderBy(b => b.Branch_Name).ToList();

            return View();
        }

        [HttpPost]
        public IActionResult EditBus(string Bus_ID, string Plate_Number, string Body_Bus_Number, string Bus_Name, string Bus_Type, string Bus_Condition, string Status)
        {
            try
            {
                // 1. Find the existing bus in the database
                var existingBus = _context.Bus.FirstOrDefault(b => b.Bus_ID == Bus_ID);

                if (existingBus == null)
                {
                    return Json(new { success = false, message = "Bus not found in the database." });
                }

                // 2. Update properties and clean up the text formatting
                existingBus.Plate_Number = Plate_Number?.Trim().ToUpper();
                existingBus.Body_Bus_Number = Body_Bus_Number?.Trim().ToUpper();
                existingBus.Bus_Name = string.IsNullOrWhiteSpace(Bus_Name) ? null : Bus_Name.Trim();
                existingBus.Bus_Type = Bus_Type;
                existingBus.Bus_Condition = Bus_Condition;

                // 3. 🛑 THE BULLETPROOF LOCK 🛑
                // Ignore the 'Status' string sent by Javascript. 
                // Force the database to calculate the true status right now!
                existingBus.Status = (Bus_Condition == "Good" || Bus_Condition == "Road Worthy") ? "Available" : "Maintenance";

                // 4. Save changes
                _context.SaveChanges();

                return Json(new { success = true, message = "Bus updated successfully!" });
            }
            catch (Exception ex)
            {
                // If the database complains, this will catch it and send the exact error back
                return Json(new { success = false, message = "Server error: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        public IActionResult Reservations()
        {
            ArchiveOldCancelledReservations();

            var userIdString = HttpContext.Session.GetString("User_ID");
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "Account");

            ViewBag.Role = HttpContext.Session.GetString("Role");

            try
            {
                // 1. Get User Details
                int currentUserId = int.Parse(userIdString);
                var currentUser = _context.Users.Include(u => u.Employee).FirstOrDefault(u => u.User_ID == currentUserId);

                // FIX: Use the full branch string (e.g., "Manila Branch") to match your DB record exactly
                var myBranchString = currentUser?.Employee?.Branch ?? "General";
                bool isBranchRestricted = (myBranchString != "General");

                // 2. Start the Reservation Query
                var resQuery = _context.Reservation
                    .Include(r => r.Trip).ThenInclude(t => t.Bus)
                    .Include(r => r.CreatedBy).ThenInclude(u => u.Employee)
                    .Where(r => r.Trip.Status != "In Transit" && r.Trip.Status != "Completed")
                    .AsQueryable();

                // 3. TABLE FILTER
                if (isBranchRestricted)
                {
                    // Now this will match "Manila Branch" == "Manila Branch"
                    resQuery = resQuery.Where(r => r.Trip != null && r.Trip.Origin == myBranchString);
                }

                ViewBag.Reservations = resQuery.OrderByDescending(r => r.Created_At).ToList();

                // 4. Teller Query (Keep as is)
                var tellerQuery = _context.Users
                    .Include(u => u.Employee)
                    .Where(u => u.Role == "Teller" || u.Role == "HeadTeller" || u.Role == "Admin")
                    .AsQueryable();

                // 5. Tellers Filter (Keep as is)
                if (isBranchRestricted)
                {
                    tellerQuery = tellerQuery.Where(u => u.Employee != null && u.Employee.Branch == myBranchString);
                }

                ViewBag.Tellers = tellerQuery.OrderBy(u => u.Username).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Reservations Error: {ex.Message}");
                ViewBag.Reservations = new List<Reservation>();
                ViewBag.Tellers = new List<PUBReservationSystem.Models.Users>();
            }

            return View();
        }

        public IActionResult UserManagement()
        {
            // 1. Session Check
            var userIdString = HttpContext.Session.GetString("User_ID");
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Account");

            // 2. Identify Current User Branch
            int currentUserId = int.Parse(userIdString);
            var currentUser = _context.Users.Include(u => u.Employee).FirstOrDefault(u => u.User_ID == currentUserId);
            int myBranchId = currentUser?.Employee?.Branch_ID ?? 0;
            var role = HttpContext.Session.GetString("Role");

            // ✅ FIXED: Determine if the logged-in user has high-level administrative clearance
            bool isAuthorizedAdmin = role == "Admin" || role == "SuperAdmin";

            // 3. Build the User Query
            var userQuery = _context.Users
                .Include(u => u.Employee)
                .AsQueryable();

            // The Master Lock for the Table (Admins and SuperAdmins can see accounts from all branches)
            if (!isAuthorizedAdmin && myBranchId > 0)
            {
                userQuery = userQuery.Where(u => u.Employee != null && u.Employee.Branch_ID == myBranchId);
            }
            ViewBag.Users = userQuery.OrderByDescending(u => u.User_ID).ToList();

            // 4. THE FIX: Fetch ALL Active Branches for the Dropdown
            var branchQuery = _context.Branches.AsQueryable();

            if (!isAuthorizedAdmin && myBranchId > 0)
            {
                // Only lock down the location if they are a lower role (like Teller or Dispatcher)
                branchQuery = branchQuery.Where(b => b.Branch_ID == myBranchId);
            }

            ViewBag.Branches = branchQuery.OrderBy(b => b.Branch_Name).ToList();

            return View();
        }

        [HttpPost]
        public JsonResult DeleteRouteForever(string routeId)
        {
            try
            {
                var route = _context.Routes.FirstOrDefault(r => r.Route_ID == routeId);

                if (route == null)
                {
                    return Json(new { success = false, message = "This route was already deleted or cannot be found." });
                }

                _context.Routes.Remove(route);
                _context.SaveChanges();

                return Json(new { success = true, message = "Route permanently deleted!" });
            }
            catch (DbUpdateConcurrencyException)
            {
                // This catches the exact error in your screenshot!
                return Json(new { success = false, message = "This route was already deleted from the database." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Database error: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult RouteManagement()
        {
            // 1. Session Check
            var userIdString = HttpContext.Session.GetString("User_ID");
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Account");

            // 2. Role Check: Maintain your existing security (Only Admins allowed here)
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index");

            ViewBag.Role = HttpContext.Session.GetString("Role");

            // 3. IDENTIFY USER & BRANCH
            int currentUserId = int.Parse(userIdString);
            var currentUser = _context.Users.Include(u => u.Employee).FirstOrDefault(u => u.User_ID == currentUserId);
            int myBranchId = currentUser?.Employee?.Branch_ID ?? 0;

            // 4. PREPARE THE QUERY
            // Start with the basic filter (Active/Not Archived)
            var routeQuery = _context.Routes
                .Where(r => r.Is_Archived == false)
                .AsQueryable();

            // 5. THE MASTER LOCK
            // If they aren't a SuperAdmin (Global Boss), lock them to their Branch_ID
            // Change "SuperAdmin" to whatever your global-access role is named
            if (HttpContext.Session.GetString("Role") != "SuperAdmin" && myBranchId > 0)
            {
                routeQuery = routeQuery.Where(r => r.Branch_ID == myBranchId);
            }

            // 6. EXECUTE
            ViewBag.Routes = routeQuery
                .OrderBy(r => r.Origin)
                .ThenBy(r => r.Destination)
                .ToList();

            return View();
        }

        [HttpPost]
        public JsonResult ArchiveRoute(string routeId)
        {
            var route = _context.Routes.FirstOrDefault(r => r.Route_ID == routeId);
            if (route != null)
            {
                // 1. You could add an 'IsArchived' boolean column to your Routes table
                // 2. OR just move it to an Archive table. 
                // Example: 
                route.Is_Archived = true;
                _context.SaveChanges();
                return Json(new { success = true, message = "Route archived successfully!" });
            }
            return Json(new { success = false, message = "Route not found." });
        }
        public IActionResult Branches()
        {
            // Fetch only ACTIVE branches from your database
            var branches = _context.Branches
                                   .Where(b => b.Is_Archived == false)
                                   .ToList();

            ViewBag.Branches = branches;

            ViewBag.Role = HttpContext.Session.GetString("Role");
            return View();
        }

        [HttpGet]
        public IActionResult AuditLogs()
        {
            // 1. Session & Basic Security Check
            var userIdString = HttpContext.Session.GetString("User_ID");
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Account");

            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin") // Still restrict to Admin role
                return RedirectToAction("Dashboard");

            ViewBag.Role = role;

            // 2. Identify Current Admin's Branch
            // We fetch the current user to get their Branch_ID
            var currentUserId = int.Parse(userIdString);
            var currentUser = _context.Users.Include(u => u.Employee).FirstOrDefault(u => u.User_ID == currentUserId);
            int myBranchId = currentUser?.Employee?.Branch_ID ?? 0;

            // 3. BUILD THE QUERY
            // We join User and Employee to get to the Branch_ID
            var logQuery = _context.AuditLog
                .Include(a => a.User)
                    .ThenInclude(u => u.Employee)
                .AsQueryable();

            // 4. THE MASTER LOCK
            // If NOT a SuperAdmin, restrict logs to those generated by users within their specific Branch
            if (role != "SuperAdmin" && myBranchId > 0)
            {
                // Only show logs where the user performing the action belongs to the Admin's branch
                logQuery = logQuery.Where(a => a.User != null &&
                                               a.User.Employee != null &&
                                               a.User.Employee.Branch_ID == myBranchId);
            }

            // 5. EXECUTE
            // Newest first, limit to 500 for performance
            var logs = logQuery
                .OrderByDescending(a => a.Timestamp)
                .Take(500)
                .ToList();

            return View(logs);
        }

        [HttpPost]
        public JsonResult DeleteBranchForever(int id)
        {
            try
            {
                // 1. Find the branch in the database
                var branch = _context.Branches.FirstOrDefault(b => b.Branch_ID == id);

                // 2. If it doesn't exist, return an error
                if (branch == null)
                {
                    return Json(new { success = false, message = "Branch not found or already deleted." });
                }

                // 3. Completely remove it from the table
                _context.Branches.Remove(branch);
                _context.SaveChanges();

                // 4. Return success to the frontend
                return Json(new { success = true, message = "Branch permanently deleted." });
            }
            catch (Exception ex)
            {
                // If the database blocks the deletion (e.g., foreign key constraints)
                return Json(new
                {
                    success = false,
                    message = "Cannot delete this branch because it is linked to active employees, routes, or records."
                });
            }
        }

        [HttpGet]
        public IActionResult GetReservationDetails(string id)
        {
            var reservation = _context.Reservation
                .Include(r => r.Trip)
                    .ThenInclude(t => t.Bus) // This loads the Bus info
                .FirstOrDefault(r => r.Reservation_ID == id);

            if (reservation == null) return Json(new { success = false });

            return Json(new
            {
                success = true,
                data = reservation
            });
        }

        [HttpPost]
        public IActionResult SaveBranch(Branch branch)
        {
            if (ModelState.IsValid)
            {
                _context.Branches.Add(branch);
                _context.SaveChanges();
                return Json(new { success = true, message = "Branch added successfully!" });
            }
            return Json(new { success = false, message = "Failed to add branch." });
        }

        // ========== GET API ==========

        [HttpGet]
        public JsonResult GetAllRoutes()
        {
            try
            {
                // 1. Get User/Branch Context
                var userIdString = HttpContext.Session.GetString("User_ID");
                if (string.IsNullOrEmpty(userIdString)) return Json(new List<object>());

                var user = _context.Users.Include(u => u.Employee).FirstOrDefault(u => u.User_ID.ToString() == userIdString);
                var myBranchString = user?.Employee?.Branch ?? "General";
                var myLocation = myBranchString.Replace(" Branch", "").Trim();
                bool isBranchRestricted = (myBranchString != "General");

                // 2. Start the Query
                var query = _context.Routes.Where(r => r.Is_Active).AsQueryable();

                // 3. 🛑 THE MASTER LOCK 🛑
                // Only show routes that originate from the admin's specific branch
                if (isBranchRestricted)
                {
                    query = query.Where(r => r.Origin == myLocation);
                }

                // 4. Execute and return
                var routes = query.OrderBy(r => r.Origin)
                    .Select(r => new
                    {
                        routeId = r.Route_ID,
                        route = r.Origin + " → " + r.Destination,
                        origin = r.Origin,
                        destination = r.Destination,
                        baseFare = r.Base_Fare
                    })
                    .ToList();

                return Json(routes);
            }
            catch { return Json(new List<object>()); }
        }

        [HttpGet]
        public JsonResult GetTripsByRoute(string origin, string destination)
        {
            try
            {
                // 1. CLEAN THE INPUTS (Remove suffixes so "Manila Branch" becomes "Manila")
                string cleanOrigin = origin.Replace(" Branch", "").Replace(" City", "").Trim();
                string cleanDest = destination.Replace(" Branch", "").Replace(" City", "").Trim();

                var trips = _context.Trip
                    .Include(t => t.Bus)
                    .Where(t =>
                        // 2. CLEAN THE DATABASE COLUMNS & COMPARE
                        t.Origin.Replace(" Branch", "").Replace(" City", "").Trim() == cleanOrigin &&
                        t.Destination.Replace(" Branch", "").Replace(" City", "").Trim() == cleanDest &&
                        (t.Status == "Scheduled" || t.Status == "Available")
                    )
                    .OrderBy(t => t.Scheduled_Departure_Time)
                    .Select(t => new
                    {
                        tripId = t.Trip_ID,
                        origin = t.Origin,
                        destination = t.Destination,
                        departure = t.Scheduled_Departure_Time.HasValue ? t.Scheduled_Departure_Time.Value.ToString("MMM dd, yyyy hh:mm tt") : "TBA",
                        baseFare = t.Base_Fare,
                        busName = t.Bus != null ? (t.Bus.Bus_Name ?? t.Bus_ID.ToString()) : "",
                        bodyNo = t.Bus != null ? t.Bus.Body_Bus_Number : "TBA"
                    })
                    .ToList();

                // 3. DEBUG LOG: If this returns 0, check your Output window in VS
                if (trips.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"DEBUG: No trips found for {cleanOrigin} to {cleanDest}");
                }

                return Json(trips);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG ERROR: {ex.Message}");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public JsonResult GetAvailableSeats(string tripId)
        {
            try
            {
                // The CRITICAL part: r.Status != "Cancelled"
                var bookedSeats = _context.Reservation
                    .Where(r => r.Trip_ID == tripId && r.Status != "Cancelled")
                    .Select(r => r.Seat_Number)
                    .ToList();

                var seats = Enumerable.Range(1, 50).Select(i => new
                {
                    seatNumber = i,
                    isBooked = bookedSeats.Contains(i)
                }).ToList();

                return Json(seats);
            }
            catch { return Json(new List<object>()); }
        }

        [HttpGet]
        public IActionResult GetDashboardStats()
        {
            var userIdString = HttpContext.Session.GetString("User_ID");
            if (string.IsNullOrEmpty(userIdString)) return Json(new { error = "Unauthorized" });

            // Ensure we are getting the branch name fresh from the user object every time
            int currentUserId = int.Parse(userIdString);
            var currentUser = _context.Users.Include(u => u.Employee).FirstOrDefault(u => u.User_ID == currentUserId);

            // If currentUser is null, do NOT return 0 (which wipes your dashboard), return an empty success or error
            if (currentUser == null || currentUser.Employee == null) return Json(new { error = "No Employee Data" });

            var myBranchString = currentUser.Employee.Branch ?? "Unknown";
            var myLocation = myBranchString.Replace(" Branch", "", StringComparison.OrdinalIgnoreCase).Trim();

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var tripQuery = _context.Trip.AsQueryable();
            var resQuery = _context.Reservation.Include(r => r.Trip).AsQueryable();

            // Use the same robust 'Contains' logic we established for the table
            if (HttpContext.Session.GetString("Role") != "SuperAdmin")
            {
                tripQuery = tripQuery.Where(t => t.Origin.Contains(myLocation) || t.Destination.Contains(myLocation));
                resQuery = resQuery.Where(r => r.Trip != null && r.Trip.Origin.Contains(myLocation));
            }

            // Return the counts
            var data = new
            {
                totalReservations = resQuery.Count(r => r.Trip != null && r.Trip.Scheduled_Departure_Time >= today && r.Trip.Scheduled_Departure_Time < tomorrow),
                scheduledTrips = tripQuery.Count(t => (t.Status == "Scheduled" || t.Status == "Active") && t.Scheduled_Departure_Time >= today && t.Scheduled_Departure_Time < tomorrow),
                inTransitBuses = tripQuery.Count(t => t.Status == "In Transit" && t.Scheduled_Departure_Time >= today && t.Scheduled_Departure_Time < tomorrow),
                cancelledTrips = tripQuery.Count(t => t.Status == "Cancelled" && t.Scheduled_Departure_Time >= today && t.Scheduled_Departure_Time < tomorrow)
            };

            return Json(data);
        }

        [HttpGet]
        public JsonResult GetTodaysScheduledTrips()
        {
            try
            {
                var today = DateTime.Today;
                var trips = _context.Trip
                    .Include(t => t.Bus)
                    .Include(t => t.Driver)
                    // FIX: Check HasValue first
                    .Where(t => t.Scheduled_Departure_Time.HasValue && t.Scheduled_Departure_Time.Value.Date == today && t.Status == "Scheduled")
                    .Select(t => new
                    {
                        tripId = t.Trip_ID,
                        busName = t.Bus != null ? (t.Bus.Bus_Name ?? t.Bus_ID) : "",
                        busBodyNumber = t.Bus != null ? t.Bus.Body_Bus_Number : "",
                        origin = t.Origin,
                        destination = t.Destination,
                        // FIX: Safely format
                        departureTime = t.Scheduled_Departure_Time.HasValue ? t.Scheduled_Departure_Time.Value.ToString("hh:mm tt") : "TBA",
                        driverName = t.Driver != null ? t.Driver.Full_Name : "Unassigned",
                        status = t.Status
                    })
                    .ToList();

                return Json(trips);
            }
            catch { return Json(new List<object>()); }
        }

        [HttpGet]
        public IActionResult GetAuditLog()
        {
            var userIdString = HttpContext.Session.GetString("User_ID");
            if (string.IsNullOrEmpty(userIdString)) return Json(new { error = "Unauthorized" });

            var role = HttpContext.Session.GetString("Role");
            int currentUserId = int.Parse(userIdString);
            var currentUser = _context.Users.Include(u => u.Employee).FirstOrDefault(u => u.User_ID == currentUserId);

            var myBranchString = currentUser?.Employee?.Branch ?? "Unknown";

            var auditQuery = _context.AuditLog.Include(a => a.User).ThenInclude(u => u.Employee).AsQueryable();

            // THE MASTER LOCK
            if (role != "SuperAdmin")
            {
                auditQuery = auditQuery.Where(a => a.User != null && a.User.Employee != null && a.User.Employee.Branch == myBranchString);
            }

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var logs = auditQuery
                .Where(a => a.Timestamp >= today && a.Timestamp < tomorrow)
                .OrderByDescending(a => a.Timestamp)
                .Take(15)
                .ToList() // Execute query before projecting to handle nulls safely
                .Select(a => new
                {
                    id = a.Log_ID,
                    action = a.Action,
                    fullName = a.User?.Employee?.Full_Name ?? a.User?.Username ?? "Unknown",
                    role = a.User?.Role ?? "Staff",
                    timestamp = a.Timestamp.ToString("hh:mm:ss tt")
                });

            return Json(logs);
        }

        [HttpGet]
        public JsonResult GetTodayIncome()
        {
            try
            {
                var today = DateTime.Today;
                var total = _context.Reservation
                    .Where(r => r.Status == "Confirmed" && r.Reservation_Date.Date == today)
                    .Sum(r => (decimal?)r.Total_Amount) ?? 0;

                return Json(new { income = total });
            }
            catch { return Json(new { income = 0 }); }
        }

        [HttpGet]
        public IActionResult GetCompletedTrips()
        {
            var userIdString = HttpContext.Session.GetString("User_ID");
            if (string.IsNullOrEmpty(userIdString)) return Json(new { error = "Unauthorized" });

            var role = HttpContext.Session.GetString("Role");
            int currentUserId = int.Parse(userIdString);
            var currentUser = _context.Users.Include(u => u.Employee).FirstOrDefault(u => u.User_ID == currentUserId);

            var myBranchString = currentUser?.Employee?.Branch ?? "Unknown";
            var myLocation = myBranchString.Replace(" Branch", "").Trim();

            var tripQuery = _context.Trip.AsQueryable();

            // THE MASTER LOCK
            if (role != "SuperAdmin")
            {
                tripQuery = tripQuery.Where(t => t.Origin == myLocation);
            }

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var trips = tripQuery
                .Where(t => t.Status == "Completed" && t.Scheduled_Departure_Time >= today && t.Scheduled_Departure_Time < tomorrow)
                .OrderByDescending(t => t.Arrival_Time)
                .Take(10)
                .ToList()
                .Select(t => new
                {
                    tripId = t.Trip_ID,
                    origin = t.Origin,
                    destination = t.Destination,
                    completedTime = t.Arrival_Time?.ToString("hh:mm tt") ?? "N/A",
                    dispatcherName = "System" // Update this if you track exactly who marked it arrived
                });

            return Json(trips);
        }

        [HttpGet]
        public JsonResult GetTrips()
        {
            int.TryParse(HttpContext.Session.GetString("Branch_ID"), out int myBranchId);
            var role = HttpContext.Session.GetString("Role");

            var query = _context.Trip
                .Include(t => t.Bus)
                .AsQueryable();

            // THE UPDATED "MASTER LOCK"
            if (role != "SuperAdmin" && myBranchId > 0)
            {
                // Logic: Show trip IF (I am the Origin) OR (I am the Destination)
                query = query.Where(t => t.Branch_ID == myBranchId || t.Destination_Branch_ID == myBranchId);
            }

            var trips = query
                .OrderBy(t => t.Scheduled_Departure_Time)
                .Select(t => new
                {
                    tripId = t.Trip_ID,
                    origin = t.Origin,
                    destination = t.Destination,
                    departureTime = t.Scheduled_Departure_Time.HasValue ? t.Scheduled_Departure_Time.Value.ToString("MMM dd, yyyy - hh:mm tt") : "TBA",
                    status = t.Status,
                    // Add a flag to UI to know if it's incoming or outgoing
                    isIncoming = (t.Destination_Branch_ID == myBranchId && t.Branch_ID != myBranchId),
                    busName = t.Bus != null ? (t.Bus.Bus_Name ?? t.Bus_ID) : "",
                    busBodyNumber = t.Bus != null ? t.Bus.Body_Bus_Number : ""
                })
                .ToList();

            return Json(trips);
        }

        [HttpGet]
        public JsonResult GetUsers()
        {
            try
            {
                var users = _context.Users
                    .AsNoTracking()
                    .Include(u => u.Employee)
                    .OrderBy(u => u.User_ID)
                    .Select(u => new
                    {
                        userId = u.User_ID,
                        username = u.Username,
                        fullName = u.Employee != null ? u.Employee.Full_Name : "",
                        role = u.Role,
                        isActive = u.Is_Active,
                        accountLocked = u.Account_Locked,
                        lastLogin = u.Last_Login.HasValue ? u.Last_Login.Value.ToString("MMM dd, yyyy hh:mm tt") : "Never"
                    })
                    .ToList();

                return Json(users);
            }
            catch { return Json(new List<object>()); }
        }

        [HttpGet]
        public JsonResult GetEmployeesByRole(string role)
        {
            try
            {
                var employees = _context.Employee
                    .Where(e => e.Job_Position == role && e.Is_Active)
                    .Select(e => new
                    {
                        employeeId = e.Employee_ID,
                        fullName = e.Full_Name,
                        jobPosition = e.Job_Position
                    })
                    .ToList();

                return Json(employees);
            }
            catch { return Json(new List<object>()); }
        }

        [HttpGet]
        public JsonResult GetRouteById(string routeId)
        {
            try
            {
                var route = _context.Routes.FirstOrDefault(r => r.Route_ID == routeId);

                if (route == null)
                {
                    return Json(new { success = false, message = "Route not found." });
                }

                // Return the object directly. 
                // Note: Ensure your property names here match your JS exactly!
                return Json(new
                {
                    success = true,
                    route_ID = route.Route_ID,
                    origin = route.Origin,
                    destination = route.Destination,
                    base_Fare = route.Base_Fare,
                    distance_KM = route.Distance_KM,
                    estimated_Hours = route.Estimated_Hours,
                    is_Active = route.Is_Active
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult CheckSessionStatus()
        {
            var userId = HttpContext.Session.GetString("User_ID");
            if (string.IsNullOrEmpty(userId))
                return Json(new { isActive = false });

            var user = _context.Users.FirstOrDefault(u => u.User_ID.ToString() == userId);
            if (user == null)
                return Json(new { isActive = false });

            return Json(new { isActive = user.Is_Active, isLocked = user.Account_Locked });
        }

        // ========== POST API ==========

        [HttpPost]
        public JsonResult CancelReservation([FromBody] CancelRequest request)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("User_ID");
                if (string.IsNullOrEmpty(userIdStr))
                    return Json(new { success = false, message = "Not logged in" });

                var role = HttpContext.Session.GetString("Role");
                if (role != "Admin" && role != "HeadTeller" && role != "Teller")
                    return Json(new { success = false, message = "Unauthorized" });

                if (string.IsNullOrEmpty(request?.reservationId))
                    return Json(new { success = false, message = "Reservation ID required" });

                var res = _context.Reservation.FirstOrDefault(r => r.Reservation_ID == request.reservationId);
                if (res == null)
                    return Json(new { success = false, message = "Reservation not found" });

                if (res.Status == "Cancelled")
                    return Json(new { success = false, message = "Already cancelled" });

                res.Status = "Cancelled";
                _context.SaveChanges();

                var userId = int.Parse(userIdStr);
                _context.AuditLog.Add(new AuditLog
                {
                    User_ID = userId,
                    Action = "CANCEL",
                    Description = $"Cancelled reservation {res.Reservation_ID} for {res.Passenger_Name} (Seat {res.Seat_Number})",
                    Timestamp = DateTime.Now
                });
                _context.SaveChanges();

                return Json(new { success = true, message = $"Reservation for {res.Passenger_Name} cancelled successfully!" });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult SaveReservation(string passengerName, string contactNumber, string passengerType,
    string idNumber, string tripId, string seatNumber, string paymentMethod, string referenceNumber)
        {
            try
            {
                // 1. Strict Validation Logic
                if (string.IsNullOrWhiteSpace(passengerName))
                    return Json(new { success = false, message = "Passenger name is required." });

                if (string.IsNullOrWhiteSpace(tripId))
                    return Json(new { success = false, message = "Trip selection is required." });

                if (string.IsNullOrWhiteSpace(seatNumber) || !int.TryParse(seatNumber, out int seatNum))
                    return Json(new { success = false, message = "A valid seat number is required." });

                var userIdStr = HttpContext.Session.GetString("User_ID");
                if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Json(new { success = false, message = "Session expired. Please log in again." });

                var trip = _context.Trip.FirstOrDefault(t => t.Trip_ID == tripId);
                if (trip == null)
                    return Json(new { success = false, message = "Selected trip was not found in the database." });

                // 2. Generate ID & Calculate Fares
                string resId = GenerateId("RES", _context.Reservation.OrderByDescending(r => r.Reservation_ID).FirstOrDefault()?.Reservation_ID, 5);

                decimal discount = passengerType == "Regular" ? 0 : 20;
                decimal discountAmount = trip.Base_Fare * (discount / 100);
                decimal totalAmount = trip.Base_Fare - discountAmount;

                // 3. Save to Database
                var newReservation = new Reservation
                {
                    Reservation_ID = resId,
                    Trip_ID = tripId,
                    User_ID = userId,
                    Passenger_Name = passengerName.Trim(),
                    Contact_Number = string.IsNullOrWhiteSpace(contactNumber) ? null : contactNumber.Trim(),
                    Passenger_Type = passengerType,
                    Discount_Percentage = discount,
                    ID_Number = string.IsNullOrWhiteSpace(idNumber) ? null : idNumber.Trim(),
                    Seat_Number = seatNum,
                    Total_Amount = totalAmount,
                    Status = "Confirmed",

                    // FIX: Use the trip's departure time as the Travel Date
                    Reservation_Date = trip.Scheduled_Departure_Time ?? DateTime.Now,

                    // Your model's default DateTime.Now will be used, 
                    // but explicitly setting it here ensures consistency.
                    Created_At = DateTime.Now
                };

                _context.Reservation.Add(newReservation);

                // 4. Audit Log
                var saleLog = new AuditLog
                {
                    User_ID = userId,
                    Action = "TICKET SALE",
                    Description = $"Processed payment of ₱{totalAmount:N2} via {paymentMethod ?? "Cash"} for Reservation #{resId} ({passengerName.Trim()}).",
                    Terminal = "System",
                    Timestamp = DateTime.Now
                };

                _context.AuditLog.Add(saleLog);
                _context.SaveChanges();

                // 5. Build Safe "Flat" DTO
                var savedReservation = _context.Reservation
                    .Include(r => r.Trip)
                        .ThenInclude(t => t.Bus)
                    .FirstOrDefault(r => r.Reservation_ID == resId);

                var safeData = new
                {
                    reservation_ID = savedReservation.Reservation_ID,
                    passenger_Name = savedReservation.Passenger_Name,
                    seat_Number = savedReservation.Seat_Number,
                    passenger_Type = savedReservation.Passenger_Type,
                    total_Amount = savedReservation.Total_Amount,
                    // Format these to show both the travel date and the creation date
                    reservation_Date = savedReservation.Reservation_Date.ToString("MMM dd, yyyy"),
                    created_At = savedReservation.Created_At.ToString("MMM dd, yyyy hh:mm tt"),

                    trip = savedReservation.Trip == null ? null : new
                    {
                        origin = savedReservation.Trip.Origin,
                        destination = savedReservation.Trip.Destination,
                        departure_Time = savedReservation.Trip.Scheduled_Departure_Time,
                        bus = savedReservation.Trip.Bus == null ? null : new
                        {
                            bus_Name = savedReservation.Trip.Bus.Bus_Name,
                            body_No = savedReservation.Trip.Bus.Body_Bus_Number
                        }
                    }
                };

                return Json(new { success = true, message = "Reservation created!", data = safeData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server Error: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetBranch(int id)
        {
            var branch = _context.Branches.FirstOrDefault(b => b.Branch_ID == id);
            if (branch == null) return Json(new { success = false });
            return Json(new { success = true, data = branch });
        }

        [HttpPost]
        public JsonResult UpdateBranch(int id, string name, string location)
        {
            var branch = _context.Branches.FirstOrDefault(b => b.Branch_ID == id);
            if (branch == null) return Json(new { success = false, message = "Branch not found." });

            branch.Branch_Name = name;
            branch.Location = location;
            _context.SaveChanges();
            return Json(new { success = true, message = "Branch updated successfully!" });
        }

        [HttpPost]
        public JsonResult ArchiveBranch(int id)
        {
            var branch = _context.Branches.FirstOrDefault(b => b.Branch_ID == id);
            if (branch == null) return Json(new { success = false, message = "Branch not found." });

            branch.Is_Archived = true; // IMPORTANT: Ensure your Branch model has an Is_Archived property
            _context.SaveChanges();
            return Json(new { success = true, message = "Branch archived!" });
        }

        [HttpPost]
        public JsonResult SaveEmployee(string fullName, string contactNumber, string gender,
                               string address, string jobPosition, string birthday, int branchId)
        {
            try
            {
                // 1. Validation: Ensure branchId is valid
                if (branchId <= 0)
                    return Json(new { success = false, message = "Please select a valid branch." });

                // 2. Efficient ID Generation (Directly on DB)
                // Since IDs are fixed length like "EMP-00001", simple string sorting works
                var last = _context.Employee.OrderByDescending(e => e.Employee_ID).FirstOrDefault();

                int nextNumber = 1;
                if (last != null && !string.IsNullOrEmpty(last.Employee_ID))
                {
                    string numberPart = last.Employee_ID.Replace("EMP-", "");
                    if (int.TryParse(numberPart, out int currentNumber))
                    {
                        nextNumber = currentNumber + 1;
                    }
                }
                string empId = "EMP-" + nextNumber.ToString("D5");

                // 3. Save to Database
                _context.Employee.Add(new Employee
                {
                    Employee_ID = empId,
                    Full_Name = fullName,
                    Contact_Number = contactNumber,
                    Gender = gender,
                    Address = string.IsNullOrEmpty(address) ? null : address,
                    Job_Position = jobPosition,

                    // CRITICAL FIX: Assign the ID and the string name if needed
                    Branch_ID = branchId,
                    // If your database requires the string name as well, keep this. 
                    // If not, you can remove it.
                    Branch = _context.Branches.FirstOrDefault(b => b.Branch_ID == branchId)?.Branch_Name,

                    Birthday = DateTime.Parse(birthday),
                    Hire_Date = DateTime.Now,
                    Is_Active = true
                });

                _context.SaveChanges();
                return Json(new { success = true, message = "Employee added successfully!", empId = empId });
            }
            catch (Exception ex)
            {
                // Return InnerException details to see the exact Foreign Key/Constraint error
                string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Error: " + msg });
            }
        }

        [HttpPost]
        public JsonResult SaveUser(string username, string password, string role, string employeeId)
        {
            try
            {
                // 1. Basic Validation
                if (string.IsNullOrWhiteSpace(employeeId))
                    return Json(new { success = false, message = "Employee ID is required." });

                if (_context.Users.Any(u => u.Username == username))
                    return Json(new { success = false, message = "Username already exists." });

                // 2. Standardize Employee ID
                string rawNumber = employeeId.ToUpper().Replace("EMP-", "").Trim();
                if (!int.TryParse(rawNumber, out int numericId))
                    return Json(new { success = false, message = "Invalid Employee ID format." });

                string standardizedEmpId = $"EMP-{numericId.ToString("D5")}";

                // 3. Look up the employee
                var employee = _context.Employee.FirstOrDefault(e => e.Employee_ID == standardizedEmpId);
                if (employee == null)
                    return Json(new { success = false, message = $"Employee {standardizedEmpId} not found." });

                // 4. Cross-Branch Prevention Lock Check
                int.TryParse(HttpContext.Session.GetString("Branch_ID"), out int adminBranchId);
                var adminRole = HttpContext.Session.GetString("Role");
                bool isAuthorizedAdmin = adminRole == "Admin" || adminRole == "SuperAdmin";

                if (!isAuthorizedAdmin && employee.Branch_ID != adminBranchId)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Security Alert: You cannot assign an employee from {employee.Branch ?? "Unknown Branch"} to your branch."
                    });
                }

                // 5. Create the User Profile with Hashed Credentials
                var newUser = new Users
                {
                    Employee_ID = employee.Employee_ID,
                    Username = username,
                    Role = role,
                    Is_Active = true,
                    Account_Locked = false,
                    Login_Attempts = 0,
                    Created_At = DateTime.Now
                    // We leave Password_Hash blank for a millisecond to let the object instantiate 
                };

                // 🔐 CRITICAL FIX: Generate the security hash block bound directly to the user identity entity
                var hasher = new PasswordHasher<Users>();
                newUser.Password_Hash = hasher.HashPassword(newUser, password);

                _context.Users.Add(newUser);
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Account created successfully for {employee.Full_Name}",
                    userId = newUser.User_ID,
                    fullName = employee.Full_Name
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SaveBus(string plateNumber, string bodyBusNumber, string busName, string condition)
        {
            try
            {
                // 1. Get Branch_ID from Session (The "Branch-Conscious" source)
                var branchIdString = HttpContext.Session.GetString("Branch_ID");
                if (string.IsNullOrEmpty(branchIdString))
                    return Json(new { success = false, message = "Session error: Branch ID not found." });

                int branchId = int.Parse(branchIdString);

                // 2. Existing validation logic
                var clean = plateNumber.Trim().ToUpper();
                if (_context.Bus.Any(b => b.Plate_Number == clean))
                    return Json(new { success = false, message = "Plate number already registered." });

                var last = _context.Bus.OrderByDescending(b => b.Bus_ID).FirstOrDefault();
                string busId = GenerateId("BUS", last?.Bus_ID, 5);

                // 3. Save with Branch_ID assignment
                _context.Bus.Add(new Bus
                {
                    Bus_ID = busId,
                    Plate_Number = clean,
                    Body_Bus_Number = bodyBusNumber.Trim().ToUpper(),
                    Bus_Name = string.IsNullOrWhiteSpace(busName) ? null : busName.Trim(),
                    Bus_Condition = condition,
                    // Checks for both in case you change the dropdown wording later
                    Status = (condition == "Good" || condition == "Road Worthy") ? "Available" : "Maintenance",
                    Created_At = DateTime.Now,
                    Branch_ID = branchId // <--- THIS WAS MISSING
                });

                _context.SaveChanges();
                return Json(new { success = true, message = "Bus added successfully!" });
            }
            catch (Exception ex)
            {
                // If it fails, this will now tell you exactly why (likely a Foreign Key violation)
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateBus(string busId, string plateNumber, string bodyBusNumber, string busName, string busType, string condition)
        {
            try
            {
                var busInDb = _context.Bus.FirstOrDefault(b => b.Bus_ID == busId);
                if (busInDb == null) return Json(new { success = false, message = "Bus not found." });

                // Update properties
                busInDb.Plate_Number = plateNumber.Trim().ToUpper();
                busInDb.Body_Bus_Number = bodyBusNumber.Trim().ToUpper();
                busInDb.Bus_Name = string.IsNullOrWhiteSpace(busName) ? null : busName.Trim();
                busInDb.Bus_Type = busType;
                busInDb.Bus_Condition = condition;

                // THE CRITICAL FIX FOR EDITS:
                busInDb.Status = (condition == "Good" || condition == "Road Worthy") ? "Available" : "Maintenance";

                _context.SaveChanges();

                return Json(new { success = true, message = "Bus updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteBus(string busId)
        {
            try
            {
                var bus = _context.Bus.FirstOrDefault(b => b.Bus_ID == busId);
                if (bus == null) return Json(new { success = false, message = "Bus not found" });

                var hasActiveTrips = _context.Trip.Any(t => t.Bus_ID == busId && (t.Status == "Scheduled" || t.Status == "In Transit"));
                if (hasActiveTrips)
                    return Json(new { success = false, message = "Cannot delete — bus has active trips." });

                _context.Bus.Remove(bus);
                _context.SaveChanges();
                return Json(new { success = true, message = "Bus deleted successfully!" });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult SaveTrip(string busId, string driverId, string conductorId,
                           string origin, string destination, string baseFare,
                           string departureTime, int originBranchId, int destinationBranchId)
        {
            try
            {
                // 1. Permission Check (Keep this, but remove the Session-based Branch assignment)
                var userIdString = HttpContext.Session.GetString("User_ID");
                if (string.IsNullOrEmpty(userIdString))
                    return Json(new { success = false, message = "Session expired." });

                // 2. Defensive Validation
                if (originBranchId == destinationBranchId)
                    return Json(new { success = false, message = "Origin and Destination cannot be the same branch." });

                if (!decimal.TryParse(baseFare, out decimal fareValue))
                    return Json(new { success = false, message = "Invalid fare amount." });

                if (!DateTime.TryParse(departureTime, out DateTime departure))
                    return Json(new { success = false, message = "Invalid departure time." });

                // 3. Logic
                string tripId = GenerateUniqueTripId();
                var status = departure < DateTime.Now ? "Completed" : "Scheduled";

                // 4. Save to Database
                _context.Trip.Add(new Trip
                {
                    Trip_ID = tripId,
                    Bus_ID = busId,
                    Employee_ID_Driver = driverId,
                    Employee_ID_Conductor = string.IsNullOrEmpty(conductorId) ? null : conductorId,
                    Origin = origin,
                    Destination = destination,
                    Base_Fare = fareValue,
                    Scheduled_Departure_Time = departure,
                    Status = status,
                    Created_At = DateTime.Now,
                    Branch_ID = originBranchId,           // NOW: Uses the ID from the Dropdown
                    Destination_Branch_ID = destinationBranchId // NOW: Uses the ID from the Dropdown
                });

                _context.SaveChanges();
                return Json(new { success = true, message = "Trip created successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }








        [HttpGet]
        public JsonResult GetRouteData()
        {
            // Fetches the routes to allow the frontend to calculate fares dynamically
            var routes = _context.Routes
                .Select(r => new
                {
                    originId = r.Origin_Branch_ID,      // Ensure these match your actual Database column names
                    destId = r.Destination_Branch_ID,
                    fare = r.Base_Fare
                })
                .ToList();

            return Json(routes);
        }
















        [HttpPost]
        [HttpPost]
        public JsonResult UpdateTripStatus(string tripId, string newStatus)
        {
            // 1. Secure an active execution transaction block context
            using var dbTransaction = _context.Database.BeginTransaction();

            try
            {
                // Validate Session & Get User Info
                var userIdStr = HttpContext.Session.GetString("User_ID");
                if (string.IsNullOrEmpty(userIdStr))
                    return Json(new { success = false, message = "Not logged in" });

                var role = HttpContext.Session.GetString("Role");
                if (role != "Dispatcher" && role != "Admin")
                    return Json(new { success = false, message = "Unauthorized" });

                int currentUserId = int.Parse(userIdStr);
                var currentBranch = HttpContext.Session.GetString("Branch") ?? "Manila";

                // Find the Trip
                var trip = _context.Trip.FirstOrDefault(t => t.Trip_ID == tripId);
                if (trip == null)
                    return Json(new { success = false, message = "Trip not found" });

                trip.Status = newStatus;

                // Log ACTUAL dispatch, arrival, and cancellation times
                if (newStatus == "In Transit")
                {
                    trip.Actual_Dispatch_Time ??= DateTime.Now;
                }
                else if (newStatus == "Completed")
                {
                    trip.Arrival_Time = DateTime.Now;
                }
                // =================================================================
                // 🛑 CASCADE CANCELLATION ENGINE INJECTION
                // =================================================================
                else if (newStatus == "Cancelled")
                {
                    trip.Cancelled_Time = DateTime.Now;

                    // 1. Fetch all active passenger reservations locked to this Trip ID
                    // (Note: If your DbContext uses plural, change '_context.Reservation' to '_context.Reservations')
                    var associatedReservations = _context.Reservation
                        .Where(r => r.Trip_ID == tripId && r.Status != "Cancelled")
                        .ToList();

                    // 2. Flip their operational status markers to Cancelled immediately
                    foreach (var res in associatedReservations)
                    {
                        res.Status = "Cancelled";
                    }

                    // 3. Batch stage the reservation records inside EF tracking cache if modifications exist
                    if (associatedReservations.Any())
                    {
                        _context.Reservation.UpdateRange(associatedReservations);

                        // Write a detailed single audit footprint entry explaining the cascade size
                        _auditService.Log(currentUserId, "Trip Disaster Cascade Void", $"Cancelled Trip {tripId} — Voided {associatedReservations.Count} passenger reservations automatically.", currentBranch);
                    }
                }

                // Force EF to track the Trip changes
                _context.Trip.Update(trip);

                // Manage Bus Status
                if (!string.IsNullOrEmpty(trip.Bus_ID))
                {
                    var bus = _context.Bus.FirstOrDefault(b => b.Bus_ID == trip.Bus_ID.Trim());
                    if (bus != null)
                    {
                        if (newStatus == "In Transit")
                        {
                            bus.Status = "In Transit";
                            _auditService.Log(currentUserId, "Dispatch Trip", $"Dispatched Trip {trip.Trip_ID}", currentBranch);
                        }
                        else if (newStatus == "Completed" || newStatus == "Cancelled" || newStatus == "Scheduled")
                        {
                            bus.Status = "Available"; // Frees up the vehicle asset instantly
                        }

                        _context.Bus.Update(bus);
                    }
                }

                // Manage Personnel
                if (!string.IsNullOrEmpty(trip.Employee_ID_Driver))
                {
                    var driver = _context.Employee.FirstOrDefault(e => e.Employee_ID == trip.Employee_ID_Driver.Trim());
                    if (driver != null && (newStatus == "Completed" || newStatus == "Cancelled" || newStatus == "Scheduled"))
                    {
                        driver.Is_Active = true;
                        _context.Employee.Update(driver);
                    }
                }

                if (!string.IsNullOrEmpty(trip.Employee_ID_Conductor))
                {
                    var conductor = _context.Employee.FirstOrDefault(e => e.Employee_ID == trip.Employee_ID_Conductor.Trim());
                    if (conductor != null && (newStatus == "Completed" || newStatus == "Cancelled" || newStatus == "Scheduled"))
                    {
                        conductor.Is_Active = true;
                        _context.Employee.Update(conductor);
                    }
                }

                // Save everything to the database AT ONCE and commit structural transaction unit
                _context.SaveChanges();
                dbTransaction.Commit();

                return Json(new
                {
                    success = true,
                    message = $"Trip status updated to {newStatus} cleanly.",
                    dispatchTime = trip.Actual_Dispatch_Time?.ToString("hh:mm tt")
                });
            }
            catch (Exception ex)
            {
                // Safe database fallback transaction rollback operation back to ground zero baseline
                dbTransaction.Rollback();
                System.Diagnostics.Debug.WriteLine($"UPDATE STATUS ERROR: {ex.Message}");
                return Json(new { success = false, message = "An internal data engine error occurred." });
            }
        }


        [HttpGet]
        public IActionResult GetLiveStats(int branchId)
        {
            // Fetch base historical dataset for the target hub center
            var trips = _context.Trip
                .Where(t => t.Branch_ID == branchId || t.Destination_Branch_ID == branchId)
                .ToList();

            // 🛑 THE MASTER FIX: Filter out Cancelled trips from the live background tracker pool
            var visibleTrips = trips
                .Where(t => t.Status != null && !t.Status.Trim().Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Json(new
            {
                success = true,
                total = visibleTrips.Count,
                scheduled = visibleTrips.Count(t => t.Status != null &&
                    (t.Status.Trim().Equals("Scheduled", StringComparison.OrdinalIgnoreCase) ||
                     t.Status.Trim().Equals("Active", StringComparison.OrdinalIgnoreCase))),
                inTransit = visibleTrips.Count(t => t.Status != null &&
                    t.Status.Trim().Equals("In Transit", StringComparison.OrdinalIgnoreCase)),
                completed = visibleTrips.Count(t => t.Status != null &&
                    t.Status.Trim().Equals("Completed", StringComparison.OrdinalIgnoreCase))
            });
        }












        // METHOD THAT THE FRONTEND CALLS TO SAVE A NEW ROUTE. THIS IS CALLED WHEN THE ADMIN CLICKS THE "SAVE" BUTTON ON THE NEW ROUTE ROW

        [HttpPost]
        public JsonResult SaveRoute(string origin, string destination, decimal baseFare, decimal? distanceKm, decimal? estimatedHours, bool isActive)
        {
            try
            {
                // 1. Get the current Admin's Branch_ID
                var branchIdString = HttpContext.Session.GetString("Branch_ID");
                if (string.IsNullOrEmpty(branchIdString))
                    return Json(new { success = false, message = "Session error: Branch ID not found." });

                int branchId = int.Parse(branchIdString);

                // 2. Check for duplicate routes
                if (_context.Routes.Any(r => r.Origin == origin && r.Destination == destination))
                    return Json(new { success = false, message = "Route already exists!" });

                var last = _context.Routes.OrderByDescending(r => r.Route_ID).FirstOrDefault();
                string routeId = GenerateId("RTE", last?.Route_ID, 4);

                // 3. Create the route object AND assign the Branch_ID
                var newRoute = new BusRoute
                {
                    Route_ID = routeId,
                    Origin = origin,
                    Destination = destination,
                    Base_Fare = baseFare,
                    Distance_KM = distanceKm,
                    Estimated_Hours = estimatedHours,
                    Is_Active = isActive,
                    Created_At = DateTime.Now,
                    Branch_ID = branchId // <--- THIS IS THE FIX
                };

                // 4. Add and Save
                _context.Routes.Add(newRoute);
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Route added successfully!",
                    data = newRoute
                });
            }
            catch (Exception ex)
            {
                // Return inner exception for easier debugging of DB constraints
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }



        // METHOD THAT THE FRONTEND CALLS TO UPDATE A ROUTE. THIS IS CALLED WHEN THE ADMIN CLICKS THE "EDIT" BUTTON ON A ROUTE ROW AFTER SAVING

        [HttpPost]
        public JsonResult UpdateRoute(string routeId, string origin, string destination, decimal baseFare, decimal? distanceKm, decimal? estimatedHours, bool isActive)
        {
            try
            {
                var route = _context.Routes.FirstOrDefault(r => r.Route_ID == routeId);
                if (route == null) return Json(new { success = false, message = "Route not found." });

                // Update the properties
                route.Origin = origin;
                route.Destination = destination;
                route.Base_Fare = baseFare;
                route.Distance_KM = distanceKm;
                route.Estimated_Hours = estimatedHours;
                route.Is_Active = isActive;
                route.Updated_At = DateTime.Now; // Optional if you have this field

                _context.SaveChanges();

                // 🔥 CRITICAL: You MUST return 'data = route' so JS can rebuild the row!
                return Json(new
                {
                    success = true,
                    message = "Route updated successfully!",
                    data = route
                });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }





        // METHOD THAT DELETE A ROUTE. THIS IS CALLED BY THE FRONTEND WHEN THE ADMIN CLICKS THE "DELETE" BUTTON ON A ROUTE ROW

        [HttpPost]
        public JsonResult DeleteRoute(string routeId)
        {
            try
            {
                var route = _context.Routes.Find(routeId);
                if (route == null) return Json(new { success = false, message = "Route not found!" });

                var hasTrips = _context.Trip.Any(t => t.Origin == route.Origin && t.Destination == route.Destination);
                if (hasTrips) return Json(new { success = false, message = "Route is used in existing trips." });

                _context.Routes.Remove(route);
                _context.SaveChanges();
                return Json(new { success = true, message = "Route deleted!" });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }





        // METHOD THAT UNLOCKS A USER ACCOUNT AFTER BEING LOCKED

        [HttpPost]
        public JsonResult UnlockUser(int userId)
        {
            try
            {
                var user = _context.Users.Find(userId);
                if (user == null) return Json(new { success = false, message = "User not found." });

                user.Account_Locked = false;
                user.Login_Attempts = 0;
                _context.SaveChanges();

                return Json(new { success = true, message = $"{user.Username} has been unlocked." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }



        // METHOD THAT ACTIVATES THE DEACIVATED USER'S ACCOUNT

        [HttpPost]
        public JsonResult ActivateUser(int userId)
        {
            try
            {
                var user = _context.Users.Find(userId);
                if (user == null) return Json(new { success = false, message = "User not found." });

                user.Is_Active = true;
                user.Account_Locked = false;
                user.Login_Attempts = 0;
                _context.SaveChanges();

                return Json(new { success = true, message = $"{user.Username} activated." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }






        // METHOD THAT THE FRONTEND CALLS TO DEACTIVATE A USER ACCOUNT. THIS IS DIFFERENT FROM "LOCKING" AN ACCOUNT

        [HttpPost]
        public JsonResult DeactivateUser(int userId)
        {
            try
            {
                var user = _context.Users.Find(userId);
                if (user == null) return Json(new { success = false, message = "User not found." });

                user.Is_Active = false;
                _context.SaveChanges();

                var adminIdStr = HttpContext.Session.GetString("User_ID");
                if (!string.IsNullOrEmpty(adminIdStr) && int.TryParse(adminIdStr, out int adminId))
                {
                    _context.AuditLog.Add(new AuditLog
                    {
                        User_ID = adminId,
                        Action = "DEACTIVATE_USER",
                        Description = $"Deactivated user: {user.Username}",
                        Timestamp = DateTime.Now
                    });
                    _context.SaveChanges();
                }

                return Json(new { success = true, message = $"{user.Username} deactivated." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }











        // METHOD THAT THE FRONTEND CALLS TO GET THE SALES REGISTRY DATA FOR THE CHARTS ON THE LEFT SIDE OF THE DASHBOARD

        [HttpGet]
        public IActionResult GetSalesRegistry()
        {
            var userIdString = HttpContext.Session.GetString("User_ID");
            if (string.IsNullOrEmpty(userIdString)) return Json(new { error = "Unauthorized" });

            var role = HttpContext.Session.GetString("Role");
            int currentUserId = int.Parse(userIdString);
            var currentUser = _context.Users.Include(u => u.Employee).FirstOrDefault(u => u.User_ID == currentUserId);

            // 1. EXACT BRANCH MATCHING
            // Do not use .Replace() here. If your DB has "Manila Branch", the Origin should be "Manila Branch".
            // If you try to strip " Branch" and the DB has the full name, the filter will fail and return 0.
            var myBranchString = currentUser?.Employee?.Branch ?? "Unknown";

            var resQuery = _context.Reservation.Include(r => r.Trip).AsQueryable();

            // 2. SAFE MASTER LOCK
            if (role != "SuperAdmin")
            {
                // Added null checks to prevent crashes if Trip or Origin is null
                resQuery = resQuery.Where(r => r.Trip != null && r.Trip.Origin == myBranchString);
            }

            var today = DateTime.Today;

            // 3. EFFICIENT DATE RANGES
            // We create a helper function/list to avoid repeating code and errors
            var result = new Dictionary<string, decimal>();

            for (int i = 0; i <= 5; i++)
            {
                var start = today.AddDays(-i);
                var end = start.AddDays(1);

                // Summing: Filter by Created_At AND Status
                var sum = resQuery
                    .Where(r => r.Created_At >= start && r.Created_At < end && r.Status != "Cancelled")
                    .Sum(r => (decimal?)r.Total_Amount) ?? 0;

                string key = (i == 0) ? "today" : $"day{i}";
                result.Add(key, sum);
            }

            // DEBUGGING STEP: Log to your Output window to see if it's filtering correctly
            System.Diagnostics.Debug.WriteLine($"DEBUG: Registry Query for {myBranchString} returned data successfully.");

            return Json(result);
        }






        // METHOD THAT THE FRONTEND CALLS EVERY 30 SECONDS TO GET THE LATEST RESERVATIONS FOR THIS BRANCH

        [HttpGet]
        public IActionResult GetLatestReservations()
        {
            // 1. Check Authorization
            var userIdString = HttpContext.Session.GetString("User_ID");
            if (string.IsNullOrEmpty(userIdString)) return Json(new { error = "Unauthorized" });

            // 2. Identify the User and Branch
            var role = HttpContext.Session.GetString("Role");
            int currentUserId = int.Parse(userIdString);
            var currentUser = _context.Users.Include(u => u.Employee).FirstOrDefault(u => u.User_ID == currentUserId);

            var myBranchString = currentUser?.Employee?.Branch ?? "Unknown";

            // 3. Start the Query
            var resQuery = _context.Reservation
                .Include(r => r.Trip).ThenInclude(t => t.Bus)
                .Include(r => r.CreatedBy).ThenInclude(u => u.Employee)
                .AsQueryable();

            // 4. THE MASTER LOCK (Branch Filtering)
            if (role != "SuperAdmin")
            {
                resQuery = resQuery.Where(r => r.Trip != null && r.Trip.Origin == myBranchString);
            }

            // 5. Fetch the 20 most recent for THIS branch
            var data = resQuery
                .OrderByDescending(r => r.Created_At)
                .Take(20)
                .Select(r => new
                {
                    resId = r.Reservation_ID,
                    tripId = r.Trip_ID,
                    busName = (r.Trip != null && r.Trip.Bus != null) ? r.Trip.Bus.Bus_Name : "Unknown Bus",
                    bodyNo = (r.Trip != null && r.Trip.Bus != null) ? r.Trip.Bus.Body_Bus_Number : "TBA",
                    route = (r.Trip != null && r.Trip.Origin != null && r.Trip.Destination != null) ? $"{r.Trip.Origin} &rarr; {r.Trip.Destination}" : "N/A",
                    createdAt = r.Created_At.ToString("MMM dd, hh:mm tt"),
                    travelDate = r.Reservation_Date.ToString("MMM dd, yyyy"),
                    depTime = (r.Trip != null && r.Trip.Scheduled_Departure_Time.HasValue) ? r.Trip.Scheduled_Departure_Time.Value.ToString("hh:mm tt").ToLower() : "—",
                    seat = r.Seat_Number,
                    type = r.Passenger_Type,
                    fare = r.Total_Amount,
                    passengerName = r.Passenger_Name,
                    contact = r.Contact_Number,
                    tellerId = r.User_ID,
                    tellerName = (r.CreatedBy != null && r.CreatedBy.Employee != null) ? r.CreatedBy.Employee.Full_Name : (r.CreatedBy != null ? r.CreatedBy.Username : "System"),
                    status = r.Status
                })
                .ToList();

            return Json(data);
        }





































        [HttpGet]
        public IActionResult Archives()
        {
            // 1. Session & Security Check
            var userIdString = HttpContext.Session.GetString("User_ID");
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Account");

            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return RedirectToAction("Dashboard");

            ViewBag.Role = role;

            // 2. Identify Current Admin's Branch
            // We retrieve the user again to get their Branch_ID
            var currentUser = _context.Users.Include(u => u.Employee).FirstOrDefault(u => u.User_ID == int.Parse(userIdString));
            int myBranchId = currentUser?.Employee?.Branch_ID ?? 0;

            // 3. FETCH ARCHIVED ROUTES (Filtered by Branch)
            // We use AsQueryable() to append the branch filter before executing
            var routeQuery = _context.Routes.Where(r => r.Is_Archived == true).AsQueryable();

            // Only restrict if they are not a SuperAdmin (adjust logic if needed)
            if (role != "SuperAdmin" && myBranchId > 0)
            {
                routeQuery = routeQuery.Where(r => r.Branch_ID == myBranchId);
            }
            ViewBag.ArchivedRoutes = routeQuery.OrderByDescending(r => r.Route_ID).ToList();

            // 4. FETCH ARCHIVED BUSES (Filtered by Branch)
            var busQuery = _context.Bus.Where(b => b.IsArchived == true).AsQueryable();

            if (role != "SuperAdmin" && myBranchId > 0)
            {
                busQuery = busQuery.Where(b => b.Branch_ID == myBranchId);
            }
            ViewBag.ArchivedBuses = busQuery.OrderByDescending(b => b.Bus_ID).ToList();

            // 5. FETCH ARCHIVED BRANCHES
            // Note: Branches usually don't have a Branch_ID FK, so we show the master list
            // If you need to restrict this too, you can add a similar filter.
            ViewBag.ArchivedBranches = _context.Branches
                                               .Where(b => b.Is_Archived == true)
                                               .OrderBy(b => b.Branch_Name)
                                               .ToList();

            return View();
        }

        [HttpPost]
        public JsonResult RestoreBranch(int id)
        {
            var branch = _context.Branches.FirstOrDefault(b => b.Branch_ID == id);
            if (branch == null) return Json(new { success = false, message = "Branch not found." });

            branch.Is_Archived = false;
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult RestoreBus(string busId)
        {
            try
            {
                var bus = _context.Bus.FirstOrDefault(b => b.Bus_ID == busId);
                if (bus == null) return Json(new { success = false, message = "Bus not found." });

                bus.IsArchived = false; // Restore it!
                _context.SaveChanges();

                return Json(new { success = true, message = "Bus successfully restored to active fleet!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RestoreArchive(string targetTab)
        {
            try
            {
                if (string.IsNullOrEmpty(targetTab))
                    return Json(new { success = false, message = "Invalid tab selection." });

                int restoredCount = 0;

                switch (targetTab.ToLower())
                {
                    case "buses":
                        // 1. Find the buses that ARE archived (true)
                        var buses = _context.Bus.Where(b => b.IsArchived == true).ToList();

                        // 2. Restore them by setting IsArchived to false
                        buses.ForEach(b => b.IsArchived = false);

                        restoredCount = buses.Count;
                        break;

                    case "routes":
                        // If your database uses BusRoutes instead of Routes, change _context.Routes to _context.BusRoutes
                        var routes = _context.Routes.Where(r => r.Is_Archived == true).ToList();
                        routes.ForEach(r => r.Is_Archived = false);
                        restoredCount = routes.Count;
                        break;

                    case "branches":
                        var branches = _context.Branches.Where(b => b.Is_Archived == true).ToList();
                        branches.ForEach(b => b.Is_Archived = false);
                        restoredCount = branches.Count;
                        break;

                    case "employees":
                        // If you have an Employees table
                        // var employees = _context.Employees.Where(e => e.Is_Archived == true).ToList();
                        // employees.ForEach(e => e.Is_Archived = false);
                        // restoredCount = employees.Count;
                        break;

                    default:
                        return Json(new { success = false, message = "Unknown archive category." });
                }

                if (restoredCount == 0)
                {
                    return Json(new { success = false, message = "No records found to restore." });
                }

                _context.SaveChanges();
                return Json(new { success = true, message = $"Successfully restored {restoredCount} record(s)!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Database error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RestoreRoute(string routeId)
        {
            try
            {
                // CHANGED: _context.BusRoutes is now _context.Routes
                var route = _context.Routes.FirstOrDefault(r => r.Route_ID == routeId);

                if (route == null)
                {
                    return Json(new { success = false, message = "Route not found in the database." });
                }

                // Set the archive flag back to false
                route.Is_Archived = false;

                _context.SaveChanges();

                return Json(new { success = true, message = "Route successfully restored!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Database error: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DeleteBusForever(string busId)
        {
            try
            {
                var bus = _context.Bus.FirstOrDefault(b => b.Bus_ID == busId);
                if (bus == null) return Json(new { success = false, message = "Bus not found." });

                _context.Bus.Remove(bus); // Actually delete it from SQL database forever
                _context.SaveChanges();

                return Json(new { success = true, message = "Bus permanently deleted." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting bus. It may be linked to past trips." });
            }
        }

        [HttpPost]
        public IActionResult ArchiveBus(string busId)
        {
            try
            {
                var bus = _context.Bus.FirstOrDefault(b => b.Bus_ID == busId);
                if (bus == null)
                {
                    return Json(new { success = false, message = "Bus not found." });
                }

                // We just flip the switch instead of deleting it!
                bus.IsArchived = true;
                _context.SaveChanges();

                return Json(new { success = true, message = "Bus archived successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ArchiveAllBuses()
        {
            try
            {
                // Get all buses that are currently NOT archived
                var activeBuses = _context.Bus.Where(b => b.IsArchived == false).ToList();

                foreach (var bus in activeBuses)
                {
                    bus.IsArchived = true;
                }

                _context.SaveChanges();

                return Json(new { success = true, message = "All buses successfully moved to Archive." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteUser(int userId)
        {
            try
            {
                var user = _context.Users.Find(userId);
                if (user == null) return Json(new { success = false, message = "User not found." });

                var username = user.Username;
                _context.Users.Remove(user);
                _context.SaveChanges();

                return Json(new { success = true, message = $"{username} deleted." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // ========== HELPERS ==========

        private string GenerateId(string prefix, string lastId, int padLength)
        {
            int next = 1;
            if (!string.IsNullOrEmpty(lastId))
            {
                var numeric = lastId.Replace(prefix + "-", "");
                if (int.TryParse(numeric, out int last)) next = last + 1;
            }
            return $"{prefix}-{next.ToString("D" + padLength)}";
        }

        private string GenerateUniqueTripId()
        {
            int next = 1;
            string id;
            do { id = "TRIP-" + next.ToString("D5"); next++; }
            while (_context.Trip.Any(t => t.Trip_ID == id));
            return id;
        }

        public class CancelRequest
        {
            public string reservationId { get; set; }
        }
    }

}