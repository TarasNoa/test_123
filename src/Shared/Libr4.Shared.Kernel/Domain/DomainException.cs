namespace Libr4.Shared.Kernel.Domain;

public class DomainException : Exception
{
    public string? Code { get; }

    public DomainException(string message) : base(message) { }
    public DomainException(string code, string message) : base(message) => Code = code;
    public DomainException(string message, Exception inner) : base(message, inner) { }
}
