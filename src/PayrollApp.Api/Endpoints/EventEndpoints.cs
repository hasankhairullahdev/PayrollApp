using Marten;
using Marten.Events;
using Microsoft.AspNetCore.Mvc;

namespace PayrollApp.Api.Endpoints;

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events");

        group.MapGet("/payroll/{id}", GetPayrollEvents)
            .WithName("GetPayrollEvents")
            .Produces<List<EventDto>>(200)
            .Produces(404);

        return app;
    }

    private static async Task<IResult> GetPayrollEvents(
        Guid id,
        IDocumentSession session,
        CancellationToken ct)
    {
        // Get event stream for this payroll run
        var events = await session.Events.FetchStreamAsync(id, token: ct);

        if (events == null || events.Count == 0)
            return Results.NotFound(new { message = "No events found for this payroll run" });

        // Map to DTOs
        var eventDtos = events.Select(e => new EventDto
        {
            Id = e.Id,
            StreamId = e.StreamId,
            Version = e.Version,
            Sequence = e.Sequence,
            EventType = e.EventType.Name,
            Timestamp = e.Timestamp,
            Data = e.Data
        }).ToList();

        return Results.Ok(eventDtos);
    }
}

public record EventDto
{
    public Guid Id { get; init; }
    public Guid StreamId { get; init; }
    public long Version { get; init; }
    public long Sequence { get; init; }
    public string EventType { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public object Data { get; init; } = new();
}

// Made with Bob