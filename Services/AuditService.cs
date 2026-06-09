// Just keep it simple
using PUBReservationSystem.Models;
namespace PUBReservationSystem.Services;

public class AuditService
{
    private readonly AppDbContext _context;

    public AuditService(AppDbContext context)
    {
        _context = context;
    }

    public void Log(int userId, string action, string description, string terminal)
    {
        var log = new AuditLog
        {
            User_ID = userId,
            Action = action,
            Description = description,
            Terminal = terminal,
            Timestamp = DateTime.Now
        };
        _context.AuditLog.Add(log);
        _context.SaveChanges();
    }
}