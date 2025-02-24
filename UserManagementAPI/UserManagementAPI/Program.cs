using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
app.Use(async (context, next) =>
{
    try
    {
        await next.Invoke();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An unhandled exception occurred.");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("An unexpected error occurred. Please try again later.");
    }
});
app.Use(async (context, next) =>
{
    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

    if (token == null)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    try
    {
        // Validate the token (you'll need to implement your own validation logic)
        var validatedToken = () => true;

        // If token is valid, proceed to the next middleware
        await next.Invoke();
    }
    catch (Exception)
    {
        // If token validation fails, return 401 Unauthorized
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    }

});
app.Use(async (context, next) =>
{
        logger.LogInformation("HTTP {Method} {Path}", context.Request.Method, context.Request.Path);
        await next.Invoke();
        logger.LogInformation("Response Status Code: {StatusCode}", context.Response.StatusCode);
});


var users = new ConcurrentDictionary<int, User>();

app.MapGet("/users", () => Results.Ok(users.Values));

app.MapGet("/users/{id}", (int id) =>
{
    if (users.TryGetValue(id, out var user))
    {
        return Results.Ok(user);
    }
    return Results.NotFound();
});

app.MapPost("/users", (User user) =>
{
    user.Id = !users.IsEmpty ? users.Keys.Max() + 1 : 1;
    users[user.Id] = user;
    return Results.Created($"/users/{user.Id}", user);
});

app.MapPut("/users/{id}", (int id, User updatedUser) =>
{
    if (users.ContainsKey(id))
    {
        updatedUser.Id = id;
        users[id] = updatedUser;
        return Results.Ok(updatedUser);
    }
    return Results.NotFound();
});

app.MapDelete("/users/{id}", (int id) =>
{
    if (users.TryRemove(id, out var user))
    {
        return Results.Ok(user);
    }
    return Results.NotFound();
});

app.Run();

record User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
}