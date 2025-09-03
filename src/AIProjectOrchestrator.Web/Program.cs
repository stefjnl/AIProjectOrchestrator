using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR;
using AIProjectOrchestrator.Web.Hubs;
using AIProjectOrchestrator.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure the web host to listen on port 8081 (matching Docker configuration)
builder.WebHost.UseUrls("http://*:8081");

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add SignalR
builder.Services.AddSignalR();

// Register API client
builder.Services.AddHttpClient<IAPIClient, APIClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetConnectionString("APIBaseUrl") ?? "http://api:8080");
});

// Register UI services
builder.Services.AddScoped<UIStateService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapHub<WorkflowHub>("/workflowhub");

app.Run();
