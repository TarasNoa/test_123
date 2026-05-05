namespace Libr4.IDE.Domain;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
