namespace PaddleBook.Api.Contracts;

public record CreateCourtDto(string Name, string Surface);
public record UpdateCourtDto(string Name, string Surface);

// Lo que devolvemos hacia fuera (podrías mapear desde la entidad)
public record CourtResponse(Guid Id, string Name, string Surface);
