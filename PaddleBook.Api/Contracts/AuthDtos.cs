namespace PaddleBook.Api.Contracts;
public record RegisterDto(string Email, string Password);
public record LoginDto(string Email, string Password);
public record AuthResult(string Message);
