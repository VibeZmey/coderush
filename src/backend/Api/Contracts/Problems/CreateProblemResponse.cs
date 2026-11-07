using System.Text.Json.Serialization;
using Domain.Models.Problems;

namespace Api.Contracts.Problems;

public sealed record CreateProblemResponse(
    Guid Id,
    Guid VersionId,
    int VersionNumber,
    [property: JsonConverter(typeof(JsonStringEnumConverter<PublicationStatus>))] PublicationStatus Status,
    DateTimeOffset CreatedAt);
