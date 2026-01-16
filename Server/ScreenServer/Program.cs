using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using ScreenServer;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------
// ✅ Logging setup — optional but useful during testing
// ----------------------------------------------------
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// ----------------------------------------------------
// ✅ Configure SignalR with detailed errors and larger
//    message size limit (for screenshot transmission)
// ----------------------------------------------------
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;               // show full exceptions to clients
    options.MaximumReceiveMessageSize = 2 * 1024 * 1024; // 2 MB limit (default was 32 KB)
});

// Memory cache for holding last screenshot of each host
builder.Services.AddMemoryCache();

// ----------------------------------------------------
// ✅ Allow all CORS requests (so viewers and agents
//    from other LAN PCs can connect easily)
// ----------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin(); // Allow all LAN clients
    });
});

// ----------------------------------------------------
// ✅ MVC + static files (optional UI or default home page)
// ----------------------------------------------------
builder.Services.AddControllersWithViews();


var app = builder.Build();

// ----------------------------------------------------
// ✅ REST API endpoint (used by old agent uploads)
// ----------------------------------------------------
app.MapPost("/api/upload", async (HttpRequest req, IMemoryCache cache, IHubContext<ScreenHub> hub) =>
{
    using var ms = new MemoryStream();
    await req.Body.CopyToAsync(ms);
    var data = ms.ToArray();
    var host = req.Headers["X-Host"].ToString() ?? "unknown";

    // Cache the screenshot for 10 minutes
    cache.Set($"screen:{host}", data, TimeSpan.FromMinutes(10));

    // Broadcast to viewers subscribed to that host group
    await hub.Clients.Group(host).SendAsync("ReceiveFrame", host, data);

    return Results.Ok();
});

// Endpoint to fetch the last cached screenshot manually
app.MapGet("/api/last/{host}", (string host, IMemoryCache cache) =>
{
    if (cache.TryGetValue<byte[]>($"screen:{host}", out var data))
        return Results.File(data, "image/jpeg");
    return Results.NotFound();
});

// ----------------------------------------------------
// ✅ Middleware pipeline
// ----------------------------------------------------
app.UseCors("AllowAll"); // must be before MapHub

// Commented out HTTPS redirection since this runs on LAN HTTP
// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// ----------------------------------------------------
// ✅ Map the SignalR hub endpoint
// ----------------------------------------------------
app.MapHub<ScreenHub>("/screenhub");

// Default MVC route (optional)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


// =====================================================================
// 🔹 ScreenHub class: Handles connections, registration, subscriptions,
//    and screenshot forwarding between Agents and Viewers.
// =====================================================================
public class ScreenHub : Hub
{
    // INTERNAL dictionary (not public!)
    internal static readonly ConcurrentDictionary<string, string> ConnectedAgents
        = new ConcurrentDictionary<string, string>();

    // Safe read-only snapshot for external services
    public static List<(string machine, string connId)> GetAgentSnapshot()
    {
        return ConnectedAgents
            .Select(k => (machine: k.Key, connId: k.Value))
            .ToList();
    }

    public Task<IEnumerable<string>> GetConnectedClients()
    {
        return Task.FromResult<IEnumerable<string>>(ConnectedAgents.Keys);
    }


    // Helper for cleanup service
    internal static bool TryRemoveAgent(string machine)
    {
        return ConnectedAgents.TryRemove(machine, out _);
    }

    private string GetLabFromMachine(string machineName)
    {
        if (string.IsNullOrWhiteSpace(machineName)) return "unknown";
        var parts = machineName.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : "unknown";
    }

    // --------------------------
    // 🔹 Register Agent
    // --------------------------
    public async Task RegisterAgent(string machineName)
    {
        ConnectedAgents[machineName] = Context.ConnectionId;  // Add/Update

        Console.WriteLine($"Agent registered → {machineName}");

        string lab = GetLabFromMachine(machineName);
        await Groups.AddToGroupAsync(Context.ConnectionId, lab);

        await Clients.All.SendAsync("ClientListUpdated",
            ConnectedAgents.Keys.ToList());
    }

    // --------------------------
    // 🔹 Viewer subscribes to a lab
    // --------------------------
    public async Task SubscribeLab(string lab)
    {
        if (!string.IsNullOrWhiteSpace(lab))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, lab);
            Console.WriteLine($"Viewer subscribed to {lab}");
        }
    }

    public async Task UnsubscribeLab(string lab)
    {
        if (!string.IsNullOrWhiteSpace(lab))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, lab);
            Console.WriteLine($"Viewer unsubscribed from {lab}");
        }
    }

    // --------------------------
    // 🔹 Screenshot relay
    // --------------------------
    public async Task SendScreenshot(string machineName, string base64Image)
    {
        string lab = GetLabFromMachine(machineName);

        await Clients.Group(machineName).SendAsync("ReceiveScreenshot",
            machineName, base64Image);

        await Clients.Group(lab).SendAsync("ReceiveScreenshot",
            machineName, base64Image);
    }

    // --------------------------
    // 🔹 Handle disconnect
    // --------------------------
    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        var agent = ConnectedAgents
            .FirstOrDefault(x => x.Value == Context.ConnectionId);

        if (!string.IsNullOrEmpty(agent.Key))
        {
            ConnectedAgents.TryRemove(agent.Key, out _);
            await Clients.All.SendAsync("ClientListUpdated",
                ConnectedAgents.Keys.ToList());
        }

        await base.OnDisconnectedAsync(ex);
    }

    public async Task SendRemoteInput(string machineName, RemoteInputCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(machineName))
            return;

        if (!ConnectedAgents.TryGetValue(machineName, out var agentConnId))
            return;

        // Forward input ONLY to that agent
        await Clients.Client(agentConnId)
            .SendAsync("RemoteInput", cmd);
    }

}

public class AgentCleanupService : BackgroundService
{
    private readonly IHubContext<ScreenHub> _hub;

    public AgentCleanupService(IHubContext<ScreenHub> hub)
    {
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Get snapshot of agents → List<(machine, connId)>
            var snapshot = ScreenHub.GetAgentSnapshot();

            foreach (var entry in snapshot)
            {
                string machine = entry.machine;
                string connId = entry.connId;

                try
                {
                    // Ping the agent
                    await _hub.Clients.Client(connId).SendAsync("Ping",
                        cancellationToken: stoppingToken);
                }
                catch
                {
                    // Ping failed → remove this agent
                    Console.WriteLine($"REMOVING stale agent → {machine}");

                    ScreenHub.TryRemoveAgent(machine);

                    await _hub.Clients.All.SendAsync(
                        "ClientListUpdated",
                        ScreenHub.ConnectedAgents.Keys.ToList());
                }
            }

            await Task.Delay(10000, stoppingToken);
        }
    }
}