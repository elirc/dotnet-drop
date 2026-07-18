using System.Net;

namespace NetDropbox.Application.Common;

public sealed class AppException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

