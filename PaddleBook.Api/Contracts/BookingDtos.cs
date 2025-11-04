namespace PaddleBook.Api.Contracts;

public record CreateBookingDto(Guid CourtId, DateTime StartTime, DateTime EndTime, string CustomerName);
public record BookingResponse(Guid Id, Guid CourtId, DateTime StartTime, DateTime EndTime, string CustomerName);
