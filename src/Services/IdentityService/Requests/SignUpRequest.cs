using Musdis.IdentityService.Models;

namespace Musdis.IdentityService.Requests;

/// <summary>
///     Represents a request for user sign-up, containing user registration information.
/// </summary>
/// 
/// <param name="UserName"> 
///     The desired username for the new user. 
/// </param>
/// <param name="Email"> 
///     The email address for the new user. 
/// </param>
/// <param name="Password"> 
///     The password for the new user's account. 
/// </param>
public record SignUpRequest(
    string UserName,
    string Email,
    string Password
)
{
    /// <summary>
    ///     Maps <see cref="SignUpRequest"/> to <see cref="User"/> object.
    /// </summary>
    /// 
    /// <returns>Corresponding user.</returns>
    internal User ToUser()
    {
        return new User
        {
            UserName = UserName,
            Email = Email
        };
    }
}