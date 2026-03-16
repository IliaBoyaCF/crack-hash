namespace Manager.Abstractions.Exceptions;

public abstract class ManagerException : Exception
{
    protected ManagerException(string message) : base(message) { }
    protected ManagerException(string message, Exception innerException) : base(message, innerException) { }
}

public class NoSuchElementException : ManagerException
{
    public NoSuchElementException(string message) : base(message)
    {
    }
    public NoSuchElementException(string message, Exception innerException) : base(message, innerException) { }
}
