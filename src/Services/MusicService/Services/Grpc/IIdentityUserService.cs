using Musdis.Common.GrpcProtos;
using Musdis.OperationResults;

namespace Musdis.MusicService.Services.Grpc;

/// <summary>
///     A service for retrieving data of users from IdentityService. 
/// </summary>
public interface IIdentityUserService
{
    /// <summary>
    ///     Gets information of user from IdentityService. 
    /// </summary>
    /// 
    /// <param name="userId">
    ///     An identifier of the user from IdentityService.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to cancel the operation.
    /// </param>
    /// 
    /// <returns>
    ///     A user information requested.
    /// </returns>
    Task<Result<UserInfo>> GetUserInfoAsync(string userId, CancellationToken cancellationToken);
}