using Microsoft.EntityFrameworkCore;

public class LoggingDbContext : DbContext
{
    public LoggingDbContext(DbContextOptions<LoggingDbContext> options) : base(options)
    {
    }

    public DbSet<RequestLog> RequestLogs => Set<RequestLog>();
}
