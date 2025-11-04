namespace PaddleBook.Domain.Entities;

public class Booking
{
    public Guid Id { get; private set; }
    public Guid CourtId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public string CustomerName { get; private set; }

    // Relación con Court
    public Court? Court { get; private set; }

    private Booking() { }

    public Booking(Guid id, Guid courtId, DateTime startTime, DateTime endTime, string customerName)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("Customer name is required.");

        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time.");

        Id = id;
        CourtId = courtId;
        StartTime = startTime;
        EndTime = endTime;
        CustomerName = customerName.Trim();
    }
}
