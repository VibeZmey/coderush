using System.Text.Json.Serialization;
using Domain.Models.Execution;

namespace Api.Contracts.Code;

public sealed record RunCodeResponse(
    Guid Id,
    [property: JsonConverter(typeof(JsonStringEnumConverter<ProcessingStatus>))] ProcessingStatus Status,
    DateTimeOffset CreatedAt);
