using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Shop.Infrastructure.Persistence;

// Exit codes are the only thing a CI/CD pipeline reads. 0 lets the deploy
// continue; anything else has to stop it before new code meets an old schema.
const int Success = 0;
const int MigrationFailed = 1;
const int MissingConfiguration = 2;

// BaseDirectory, not GetCurrentDirectory: `dotnet run --project x` keeps the
// caller's working directory, so the json would be looked for in the wrong
// place - and optional:true would hide that by silently skipping it.
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine(
        "No connection string. Set ConnectionStrings__DefaultConnection, " +
        "or place an appsettings.json next to the executable.");
    return MissingConfiguration;
}

// Announce the target so a failed run says which database it was pointed at -
// host and database only, never the password.
var csb = new NpgsqlConnectionStringBuilder(connectionString);
Console.WriteLine($"Target : {csb.Host}:{csb.Port}/{csb.Database}");

// Built by hand: there is no generic host and no DI container in this process.
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql(connectionString)
    .Options;

await using var dbContext = new ApplicationDbContext(options);

try
{
    // Report the state before changing anything. This job runs unattended, so
    // the log is the only witness when it fails.
    var applied = (await dbContext.Database.GetAppliedMigrationsAsync()).ToList();
    var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();

    Console.WriteLine($"Applied: {applied.Count}");
    foreach (var migration in applied)
    {
        Console.WriteLine($"  - {migration}");
    }

    if (pending.Count == 0)
    {
        Console.WriteLine("Pending: none - database is up to date.");
        return Success;
    }

    Console.WriteLine($"Pending: {pending.Count}");
    foreach (var migration in pending)
    {
        Console.WriteLine($"  + {migration}");
    }

    // Safe to run concurrently: EF Core locks __EFMigrationsHistory, so a second
    // runner waits and then finds nothing to do. That same lock is why this must
    // NOT live in API startup - five pods racing for it is a CrashLoopBackOff.
    Console.WriteLine("Applying...");
    await dbContext.Database.MigrateAsync();

    Console.WriteLine($"Done. {pending.Count} migration(s) applied.");
    return Success;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Migration failed: {ex.Message}");

    // The useful detail is almost always one level down: a Npgsql socket error,
    // a permission denied, a duplicate table.
    if (ex.InnerException is not null)
    {
        Console.Error.WriteLine($"  caused by: {ex.InnerException.Message}");
    }

    return MigrationFailed;
}
