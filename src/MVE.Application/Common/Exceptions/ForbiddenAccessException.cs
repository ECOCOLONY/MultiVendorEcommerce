namespace Application.Common.Exceptions;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base("You are not allowed to perform this action.") { }
    public ForbiddenAccessException(string message) : base(message) { }
}
