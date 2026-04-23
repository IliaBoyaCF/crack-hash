namespace Manager.Api.Exceptions;

public class ApiException : Exception
{
    public int StatusCode { get; }

    protected ApiException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

}

public class NotFoundException(string message) : ApiException(message, 404);

public class ValidationException(string message) : ApiException(message, 400);

public class QueueOverflowException(string message) : ApiException(message, 503);
