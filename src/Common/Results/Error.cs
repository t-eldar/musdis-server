namespace Results;

/// <summary>
/// Represents error in Results pattern.
/// </summary>
public class Error
{
    /// <summary>
    /// Gets the error code.
    /// </summary>
    public int Code { get; private set; }

    /// <summary>
    /// Gets the description of the error.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> class with the specified error code and description.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="description">The description of the error.</param>
    public Error(int code, string description)
    {
        Code = code;
        Description = description;
    }
}