namespace PaddleBook.Api.Contracts;

public record CourtQueryParams(
    int Page = 1,
    int PageSize = 10,
    string? Surface = null,
    string? Search = null
);
