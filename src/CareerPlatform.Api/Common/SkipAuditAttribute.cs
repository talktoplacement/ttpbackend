namespace CareerPlatform.Api.Common;

/// <summary>
/// Opt a controller or action OUT of automatic audit-log emission. Use on high-frequency or
/// low-value mutations (e.g. read-marking notifications) where an audit row per call would be
/// noise rather than signal.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class SkipAuditAttribute : Attribute
{
}
