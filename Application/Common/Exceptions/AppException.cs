namespace ScentMatch.Application.Common.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }
}

public class BadRequestException : AppException
{
    public BadRequestException(string message) : base(message, AppStatusCodes.BadRequest)
    {
    }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message, AppStatusCodes.Unauthorized)
    {
    }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message, AppStatusCodes.Forbidden)
    {
    }
}

public class ConflictException : AppException
{
    public ConflictException(string message) : base(message, AppStatusCodes.Conflict)
    {
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message, AppStatusCodes.NotFound)
    {
    }
}

public static class AppStatusCodes
{
    public const int BadRequest = 400;
    public const int Unauthorized = 401;
    public const int Forbidden = 403;
    public const int NotFound = 404;
    public const int Conflict = 409;
    public const int InternalServerError = 500;
}
