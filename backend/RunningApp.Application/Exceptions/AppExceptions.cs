namespace RunningApp.Application.Exceptions;

/// <summary>
/// Thrown when a requested resource (preview, plan, training day, decision, etc.)
/// does not exist for the current user. Mapped to HTTP 404 by the API's global
/// exception handler.
/// </summary>
public sealed class NotFoundAppException : Exception
{
    public NotFoundAppException(string message) : base(message) { }
}

/// <summary>
/// Thrown when an operation cannot proceed because of the current state of a
/// resource (e.g. an expired preview, an already-active plan). Mapped to
/// HTTP 409 by the API's global exception handler.
/// </summary>
public sealed class ConflictAppException : Exception
{
    public ConflictAppException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a protected endpoint is accessed without a valid Firebase ID
/// Token. Mapped to HTTP 401 by the API's global exception handler.
/// This is raised by <c>FirebaseIdentityProvider</c> when no verified identity
/// exists on the current request (missing Authorization header).
/// </summary>
public sealed class UnauthorizedAppException : Exception
{
    public UnauthorizedAppException(string message) : base(message) { }
}
