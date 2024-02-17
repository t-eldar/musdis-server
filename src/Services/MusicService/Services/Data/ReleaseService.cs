using FluentValidation;

using Microsoft.EntityFrameworkCore;

using Musdis.MusicService.Data;
using Musdis.MusicService.Models;
using Musdis.MusicService.Requests;
using Musdis.MusicService.Services.Utils;
using Musdis.OperationResults;
using Musdis.OperationResults.Extensions;
using Musdis.ResponseHelpers.Errors;

namespace Musdis.MusicService.Services.Data;

public sealed class ReleaseService : IReleaseService
{
    private readonly ITrackService _trackService;
    private readonly MusicServiceDbContext _dbContext;
    private readonly ISlugGenerator _slugGenerator;
    private readonly IValidator<CreateReleaseRequest> _createValidator;
    private readonly IValidator<UpdateReleaseRequest> _updateValidator;

    public ReleaseService(
        MusicServiceDbContext dbContext,
        ISlugGenerator slugGenerator,
        ITrackService trackService,
        IValidator<CreateReleaseRequest> createValidator,
        IValidator<UpdateReleaseRequest> updateValidator
    )
    {
        _dbContext = dbContext;
        _slugGenerator = slugGenerator;
        _trackService = trackService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<Release>> CreateAsync(
        CreateReleaseRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new ValidationError(
                "Cannot create Release, incorrect data!",
                validationResult.Errors.Select(e => e.ErrorMessage)
            ).ToValueResult<Release>();
        }

        var releaseType = await _dbContext.ReleaseTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Slug == request.ReleaseTypeSlug, cancellationToken);
        if (releaseType is null)
        {
            return new ValidationError(
                "Cannot create Release, ReleaseTypeSlug is invalid."
            ).ToValueResult<Release>();
        }

        var slugResult = await _slugGenerator.GenerateUniqueSlugAsync<Release>(request.Name, cancellationToken);
        if (slugResult.IsFailure)
        {
            return new InternalServerError(
                "Cannot generate slug for Release while creating."
            ).ToValueResult<Release>();
        }

        var release = new Release
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            ReleaseTypeId = releaseType.Id,
            Slug = slugResult.Value,
            ReleaseDate = DateTime.Parse(request.ReleaseDate),
            CoverUrl = request.CoverUrl
        };

        var addArtistsResult = await AddReleaseArtistsAsync(release, request.ArtistIds, cancellationToken);
        if (addArtistsResult.IsFailure)
        {
            return addArtistsResult.Error.ToValueResult<Release>();
        }

        var addTracksResult = await AddReleaseTracksAsync(release, request.Tracks, cancellationToken);
        if (addTracksResult.IsFailure)
        {
            return addTracksResult.Error.ToValueResult<Release>();
        }

        await _dbContext.Releases.AddAsync(release, cancellationToken);
        await _dbContext.Entry(release).Reference(r => r.Artists).LoadAsync(cancellationToken);

        return release.ToValueResult();
    }

    public async Task<Result> DeleteAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default
    )
    {
        var release = await _dbContext.Releases
            .FirstOrDefaultAsync(a => a.Id == releaseId, cancellationToken);
        if (release is null)
        {
            return new NoContentError(
                $"Could not able to delete artist, content with Id={releaseId} not found."
            ).ToResult();
        }

        _dbContext.Releases.Remove(release);

        return Result.Success();
    }

    public async Task<Result<Release>> UpdateAsync(
        Guid id,
        UpdateReleaseRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new ValidationError(
                "Cannot update release, incorrect data",
                validationResult.Errors.Select(f => f.ErrorMessage)
            ).ToValueResult<Release>();
        }

        var release = await _dbContext.Releases
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (release is null)
        {
            return new NotFoundError(
                $"Release with Id = {id} is not found."
            ).ToValueResult<Release>();
        }

        if (request.Name is not null)
        {
            var slugResult = await _slugGenerator.GenerateUniqueSlugAsync<Release>(
                request.Name,
                cancellationToken
            );
            if (slugResult.IsFailure)
            {
                return slugResult.Error.ToValueResult<Release>();
            }

            release.Name = request.Name;
            release.Slug = slugResult.Value;
        }
        if (request.ReleaseTypeSlug is not null)
        {
            var releaseType = await _dbContext.ReleaseTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Slug == request.ReleaseTypeSlug, cancellationToken);
            if (releaseType is null)
            {
                return new InternalServerError(
                    $"Cannot update release, release type with slug = {request.ReleaseTypeSlug} is not found."
                ).ToValueResult<Release>();
            }

            release.ReleaseTypeId = releaseType.Id;
        }

        if (request.ReleaseDate is not null)
        {
            release.ReleaseDate = DateTime.Parse(request.ReleaseDate);
        }

        release.CoverUrl = request.CoverUrl ?? release.CoverUrl;
        if (request.ArtistIds is not null)
        {
            var updateArtistsResult = await UpdateReleaseArtistsAsync(
                release,
                request.ArtistIds,
                cancellationToken
            );
            if (updateArtistsResult.IsFailure)
            {
                return updateArtistsResult.Error.ToValueResult<Release>();
            }
        }

        return release.ToValueResult();
    }

    public IQueryable<Release> GetQueryable()
    {
        return _dbContext.Releases.AsNoTracking();
    }

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return new InternalServerError(
                $"Cannot save changes to database: {ex.Message}"
            ).ToResult();
        }
    }

    // Helper methods
    private async Task<Result> AddReleaseTracksAsync(
        Release release,
        IEnumerable<CreateReleaseRequest.TrackInfo> trackInfos,
        CancellationToken cancellationToken
    )
    {
        if (release?.Artists is null)
        {
            throw new ArgumentException("Cannot access Artists property in creating Release", nameof(release));
        }

        try
        {
            foreach (var trackInfo in trackInfos)
            {
                var artistIds = trackInfo.ArtistIds ?? release.Artists.Select(a => a.Id);

                var result = await _trackService.CreateAsync(
                    new CreateTrackRequest(
                        trackInfo.Title,
                        release.Id,
                        artistIds,
                        trackInfo.TagSlugs
                    ),
                    cancellationToken
                );
                if (result.IsFailure)
                {
                    return result.Error.ToResult();
                }
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    private async Task<Result> AddReleaseArtistsAsync(
        Release release,
        IEnumerable<Guid> artistIds,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var artists = await _dbContext.Artists
                .Where(a => artistIds.Contains(a.Id))
                .ToListAsync(cancellationToken);

            release.Artists = [];
            foreach (var artist in artists)
            {
                release.Artists.Add(artist);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    private async Task<Result> UpdateReleaseArtistsAsync(
        Release release,
        IEnumerable<Guid> artistIds,
        CancellationToken cancellationToken
    )
    {
        if (release?.Artists is null || artistIds is null)
        {
            return new InternalServerError(
                "Couldn't update artist users"
            ).ToResult();
        }

        try
        {
            var artists = await _dbContext.Artists
                .AsNoTracking()
                .Where(a => artistIds.Contains(a.Id))
                .ToListAsync(cancellationToken);

            release.Artists.Clear();
            foreach (var artist in artists)
            {
                release.Artists.Add(artist);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return new InternalServerError(
                $"Couldn't update track artists: {ex.Message}"
            ).ToResult();
        }
    }
}
