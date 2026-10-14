using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var info = app.Configuration.GetSection("Service").Get<ServiceInfo>() ?? new ServiceInfo();
var circuits = new ConcurrentDictionary<string, Circuit>(StringComparer.OrdinalIgnoreCase);
circuits["CKT-0001"] = new Circuit("CKT-0001", "KUL-01", "PNG-01", "TM", 1000, "Active");
var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Active", "Planned", "Decommissioned" };

app.MapGet("/", () => Results.Ok(new { service = info.Name }));
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapGet("/version", () => Results.Ok(new {
    service = info.Name,
    version = info.Version,
    commitSha = info.CommitSha,
    buildTime = info.BuildTime,
    environment = app.Environment.EnvironmentName,
}));
app.MapGet("/circuits", () => circuits.Values.OrderBy(circuit => circuit.CircuitId));
app.MapGet("/circuits/{id}", (string id) => circuits.TryGetValue(id, out var circuit) ? Results.Ok(circuit) : Results.NotFound());
app.MapPost("/circuits", (Circuit circuit) => {
    if (string.IsNullOrWhiteSpace(circuit.CircuitId) || string.IsNullOrWhiteSpace(circuit.ASite) || string.IsNullOrWhiteSpace(circuit.ZSite)) {
        return Results.Problem(title: "Circuit ID and both sites are required.", statusCode: StatusCodes.Status400BadRequest);
    }

    if (!allowed.Contains(circuit.Status)) {
        return Results.Problem(title: "Status must be Active, Planned, or Decommissioned.", statusCode: StatusCodes.Status400BadRequest);
    }

    if (!circuits.TryAdd(circuit.CircuitId.Trim(), circuit with { CircuitId = circuit.CircuitId.Trim() })) {
        return Results.Problem(title: "Circuit already exists.", statusCode: StatusCodes.Status409Conflict);
    }

    return Results.Created($"/circuits/{circuit.CircuitId.Trim()}", circuits[circuit.CircuitId.Trim()]);
});
app.MapPost("/circuits/{id}/status", (string id, StatusChange change) => {
    if (!circuits.TryGetValue(id, out var circuit)) {
        return Results.NotFound();
    }

    if (!allowed.Contains(change.Status)) {
        return Results.Problem(title: "Status must be Active, Planned, or Decommissioned.", statusCode: StatusCodes.Status400BadRequest);
    }

    var updated = circuit with { Status = change.Status };
    circuits[id] = updated;
    return Results.Ok(updated);
});

// Deliberately insecure. Present only on demo/vulnerable so scanners have something to report.
app.MapGet("/circuits/search", (string? name) => {
    var query = "SELECT * FROM circuits WHERE name = '" + name + "'";
    var hash = Convert.ToHexString(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(name ?? "demo")));
    return Results.Ok(new { query, hash });
});

app.Run();

public sealed record Circuit(string CircuitId, string ASite, string ZSite, string Provider, int BandwidthMbps, string Status);
public sealed record StatusChange(string Status);

public sealed class ServiceInfo {
    public string Name { get; set; } = "circuit-service";
    public string Version { get; set; } = "v0.0.0-dev";
    public string CommitSha { get; set; } = "local";
    public string BuildTime { get; set; } = "unknown";
}

public partial class Program;
