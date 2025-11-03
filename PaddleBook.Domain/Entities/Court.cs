namespace PaddleBook.Domain.Entities;

public class Court
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Surface { get; private set; }

    // EF Core necesita un ctor sin parámetros (puede ser private)
    private Court() { }

    public Court(Guid id, string name, string surface)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required");
        Id = id;
        Name = name.Trim();
        Surface = string.IsNullOrWhiteSpace(surface) ? "unknown" : surface.Trim();
    }
}
