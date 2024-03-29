using Microsoft.AspNetCore.Http;

using Musdis.OperationResults;

namespace Musdis.ResponseHelpers.Errors;

/// <summary>
///     Represents an error indicating that access to the target resource is no longer available, 
///     associated with an HTTP status code 410.
/// </summary>
public sealed class GoneError : HttpError
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GoneError"/> class.
    /// </summary>
    /// 
    /// <param name="description">
    ///     A description providing additional information about the error.
    /// </param>
    public GoneError(string description) : base(
        StatusCodes.Status410Gone,
        description,
        ProblemDetailsType,
        ErrorTitle
    ) { }

    /// <inheritdoc cref="GoneError.GoneError(string)"/>
    public GoneError() : this(ErrorTitle) { }

    /// <inheritdoc cref="HttpError.ErrorType"/>
    public static readonly string ProblemDetailsType = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1";

    /// <inheritdoc cref="HttpError.Title"/>
    public static readonly string ErrorTitle = "Access to the target resource is no longer available!";

}