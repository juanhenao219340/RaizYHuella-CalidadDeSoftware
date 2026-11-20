namespace Veterinaria.Domain.Rules;

public sealed record DomainResult
{
    public DomainError? Error { get; private init; }

    public bool IsSuccess => Error is null;

    public static DomainResult Success() => new();

    public static DomainResult Failed(string code, string message) =>
        new() { Error = new DomainError(code, message) };

    public void ThrowIfFailed()
    {
        if (Error is not null)
        {
            throw new DomainRuleException(Error.Code, Error.Message);
        }
    }
}
