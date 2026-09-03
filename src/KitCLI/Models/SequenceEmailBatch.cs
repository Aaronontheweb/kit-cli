using System.Text.Json.Serialization;

namespace KitCLI.Models;

/// <summary>
/// A reviewed batch of field-scoped sequence-email edits, loaded from a local JSON manifest.
/// Unknown members are rejected (<see cref="JsonUnmappedMemberHandling.Disallow"/>) so a manifest
/// can never smuggle in a field that broadens the mutation scope beyond <c>subject</c>/<c>content</c>.
/// This type is deserialized from user-authored JSON and serialized by the manifest generator.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SequenceEmailBatchManifest
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    // Nullable because an explicit JSON "items": null overwrites the initializer; validation rejects it.
    [JsonPropertyName("items")]
    public SequenceEmailBatchManifestItem[]? Items { get; set; } = [];
}

/// <summary>
/// One independently reviewable, single-field replacement in a
/// <see cref="SequenceEmailBatchManifest"/>. Exactly one target field (<c>subject</c> or
/// <c>content</c>) is edited; every other value is an expectation that is verified but never sent.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SequenceEmailBatchManifestItem
{
    [JsonPropertyName("sequence_id")]
    public long SequenceId { get; set; }

    /// <summary>Expected parent sequence name; preflight aborts the whole batch if it differs.</summary>
    [JsonPropertyName("expected_sequence_name")]
    public string? ExpectedSequenceName { get; set; }

    [JsonPropertyName("email_id")]
    public long EmailId { get; set; }

    /// <summary><c>subject</c> or <c>content</c>.</summary>
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    /// <summary>New subject text (subject rows only).</summary>
    [JsonPropertyName("replacement")]
    public string? Replacement { get; set; }

    /// <summary>Concurrency guard for subject rows: abort unless the live subject matches this.</summary>
    [JsonPropertyName("expect_subject")]
    public string? ExpectSubject { get; set; }

    /// <summary>Local reviewed HTML file (content rows only), resolved relative to the manifest.</summary>
    [JsonPropertyName("content_file")]
    public string? ContentFile { get; set; }

    /// <summary>Concurrency guard for content rows: abort unless the live body SHA-256 matches this.</summary>
    [JsonPropertyName("expect_content_sha256")]
    public string? ExpectContentSha256 { get; set; }

    /// <summary>Expected publish state; preflight aborts if the live value differs (never sent).</summary>
    [JsonPropertyName("expected_published")]
    public bool? ExpectedPublished { get; set; }

    /// <summary>Expected position; preflight aborts if the live value differs (never sent).</summary>
    [JsonPropertyName("expected_position")]
    public int? ExpectedPosition { get; set; }
}

/// <summary>
/// Machine-readable audit report for a <c>sequence email update-batch</c> run. Body content is
/// never included verbatim — only byte counts and SHA-256 fingerprints. The manifest hash is
/// recorded as provenance (what bytes ran), not used as an apply gate.
/// </summary>
public sealed class SequenceEmailBatchReport
{
    [JsonPropertyName("manifest_sha256")]
    public string ManifestSha256 { get; set; } = string.Empty;

    [JsonPropertyName("manifest_name")]
    public string? ManifestName { get; set; }

    [JsonPropertyName("tool_version")]
    public string? ToolVersion { get; set; }

    [JsonPropertyName("profile")]
    public string? Profile { get; set; }

    /// <summary><c>dry-run</c> or <c>apply</c>.</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = string.Empty;

    [JsonPropertyName("started_at")]
    public DateTimeOffset StartedAt { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    [JsonPropertyName("preflighted")]
    public int Preflighted { get; set; }

    /// <summary>Rows written and read-back verified (a row is only counted here after verification passes).</summary>
    [JsonPropertyName("updated")]
    public int Updated { get; set; }

    [JsonPropertyName("skipped")]
    public int Skipped { get; set; }

    [JsonPropertyName("failed")]
    public int Failed { get; set; }

    /// <summary>Explicit statement of the delivery-sensitive changes that were neither requested nor observed.</summary>
    [JsonPropertyName("scope_statement")]
    public string ScopeStatement { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public SequenceEmailBatchItemReport[] Items { get; set; } = [];
}

/// <summary>Per-row entry in a <see cref="SequenceEmailBatchReport"/>.</summary>
public sealed class SequenceEmailBatchItemReport
{
    [JsonPropertyName("sequence_id")]
    public long SequenceId { get; set; }

    [JsonPropertyName("email_id")]
    public long EmailId { get; set; }

    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    /// <summary>preflight-failed, preflight-ok, no-change, applied, failed, skipped, or resumed.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("changed")]
    public bool Changed { get; set; }

    [JsonPropertyName("failure_reason")]
    public string? FailureReason { get; set; }

    [JsonPropertyName("subject_before")]
    public string? SubjectBefore { get; set; }

    [JsonPropertyName("subject_after")]
    public string? SubjectAfter { get; set; }

    [JsonPropertyName("content_bytes_before")]
    public int? ContentBytesBefore { get; set; }

    [JsonPropertyName("content_bytes_after")]
    public int? ContentBytesAfter { get; set; }

    [JsonPropertyName("content_sha256_before")]
    public string? ContentSha256Before { get; set; }

    [JsonPropertyName("content_sha256_after")]
    public string? ContentSha256After { get; set; }
}
