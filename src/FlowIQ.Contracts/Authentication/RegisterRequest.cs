namespace FlowIQ.Contracts.Authentication;

public record RegisterRequest(
    string CompanyName,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? Source = null);
