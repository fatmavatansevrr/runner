namespace RunningApp.Application.RuntimeCatalog.Schedule;

internal abstract class CatalogPublicMaterializationException : InvalidOperationException
{
    public string Code { get; }

    protected CatalogPublicMaterializationException(string code, string message) : base(message)
    {
        Code = code;
    }
}

internal sealed class CatalogPublicWorkoutTypeUnsupportedException : CatalogPublicMaterializationException
{
    public CatalogPublicWorkoutTypeUnsupportedException(string message) : base("CATALOG_PUBLIC_WORKOUT_TYPE_UNSUPPORTED", message) { }
}

internal sealed class CatalogPublicPaceRepresentationUnsupportedException : CatalogPublicMaterializationException
{
    public CatalogPublicPaceRepresentationUnsupportedException(string message) : base("CATALOG_PUBLIC_PACE_REPRESENTATION_UNSUPPORTED", message) { }
}

internal sealed class CatalogPublicSegmentUnsupportedException : CatalogPublicMaterializationException
{
    public CatalogPublicSegmentUnsupportedException(string message) : base("CATALOG_PUBLIC_SEGMENT_UNSUPPORTED", message) { }
}

internal sealed class CatalogPublicDistanceMismatchException : CatalogPublicMaterializationException
{
    public CatalogPublicDistanceMismatchException(string message) : base("CATALOG_PUBLIC_DISTANCE_MISMATCH", message) { }
}

internal sealed class CatalogPublicDurationMappingException : CatalogPublicMaterializationException
{
    public CatalogPublicDurationMappingException(string message) : base("CATALOG_PUBLIC_DURATION_MAPPING", message) { }
}

internal sealed class CatalogPublicPayloadInvalidException : CatalogPublicMaterializationException
{
    public CatalogPublicPayloadInvalidException(string message) : base("CATALOG_PUBLIC_PAYLOAD_INVALID", message) { }
}

internal sealed class CatalogPublicCanonicalSerializationException : CatalogPublicMaterializationException
{
    public CatalogPublicCanonicalSerializationException(string message) : base("CATALOG_PUBLIC_CANONICAL_SERIALIZATION", message) { }
}
