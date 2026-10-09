using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using RunningApp.Api.ErrorHandling;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.ErrorHandling;

/// <summary>
/// PHASE V1-HARDEN.2 (V1HARDEN0-006) -- proves that the handful of non-500
/// typed exceptions the V1-HARDEN.0 audit (§38-40) found leaking internal
/// catalog/engineering vocabulary verbatim via <c>exception.Message</c> now
/// return a safe, generic, product-appropriate message instead, while the
/// HTTP status code and machine-readable ErrorCode are completely
/// unchanged (so no client contract break), and the original internal
/// detail is still available server-side via the exception passed to
/// ILogger (not re-asserted here; that's standard ILogger plumbing, not new
/// infrastructure).
/// </summary>
public sealed class GlobalExceptionHandlerSafeMessageTests
{
    private static async Task<(int StatusCode, JsonElement Body)> InvokeAsync(System.Exception exception)
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(context, exception, default);
        Assert.True(handled);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, doc.RootElement.Clone());
    }

    [Fact]
    public async Task CatalogCandidateNotPublished_NeverLeaksCandidateKeyOrStatusVocabulary()
    {
        var exception = new CatalogCandidateNotPublishedException(
            "Catalog candidate TEN_K__4D__INTERMEDIATE v1 has status 'DRAFT', not 'PUBLISHED'. " +
            "It is not eligible for public preview.");

        var (statusCode, body) = await InvokeAsync(exception);

        Assert.Equal(StatusCodes.Status409Conflict, statusCode);
        Assert.Equal("CATALOG_CANDIDATE_NOT_PUBLISHED", body.GetProperty("errorCode").GetString());
        var message = body.GetProperty("message").GetString();
        Assert.DoesNotContain("DRAFT", message);
        Assert.DoesNotContain("PUBLISHED", message);
        Assert.DoesNotContain("candidate", message);
    }

    [Fact]
    public async Task CatalogDependencyNotRuntimeEligible_NeverLeaksInternalVocabulary()
    {
        var exception = new CatalogDependencyNotRuntimeEligibleException(
            "Catalog candidate X v1's dependency 'MASTER_TEMPLATE' has status 'DRAFT', not 'PUBLISHED'.");

        var (statusCode, body) = await InvokeAsync(exception);

        Assert.Equal(StatusCodes.Status409Conflict, statusCode);
        Assert.Equal("CATALOG_DEPENDENCY_NOT_RUNTIME_ELIGIBLE", body.GetProperty("errorCode").GetString());
        var message = body.GetProperty("message").GetString();
        Assert.DoesNotContain("DRAFT", message);
        Assert.DoesNotContain("dependency", message);
    }

    [Fact]
    public async Task CatalogPreviewPersistenceContract_NeverLeaksInternalCatalogVocabulary()
    {
        var exception = new CatalogPreviewPersistenceContractException(
            "Plan preview 'abc' represents the known unsupported 8-week explicit-zero weekly-volume path " +
            "(taper-week residual volume falls below the V1 key/easy session-allocation floor) and cannot be confirmed.");

        var (statusCode, body) = await InvokeAsync(exception);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, statusCode);
        Assert.Equal("CATALOG_PREVIEW_PERSISTENCE_CONTRACT", body.GetProperty("errorCode").GetString());
        var message = body.GetProperty("message").GetString();
        Assert.DoesNotContain("taper-week", message);
        Assert.DoesNotContain("session-allocation floor", message);
    }

    [Fact]
    public async Task CorrelationId_StillPresent_OnSanitizedResponses()
    {
        var exception = new CatalogCandidateNotPublishedException("internal text");
        var (_, body) = await InvokeAsync(exception);

        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("correlationId").GetString()));
    }

    [Fact]
    public async Task UnrelatedTypedException_StillUsesExceptionMessage_NoRegressionToExistingBehavior()
    {
        // Sanity: this fix is scoped to the handful of named error codes --
        // every other existing non-500 typed exception's Message passthrough
        // is unchanged (V1-HARDEN.2 is additive, not a taxonomy redesign).
        var exception = new UnsupportedTargetDistanceException("target_distance_km 4.9 is not supported.");
        var (statusCode, body) = await InvokeAsync(exception);

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        Assert.Equal("target_distance_km 4.9 is not supported.", body.GetProperty("message").GetString());
    }
}
