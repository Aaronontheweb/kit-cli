# Kit CLI 1.7.1 — Implementation Plan

Guarded batch sequence-email authoring + manifest generate helper.

Spec of record: memorizer `85efc03a-8860-4bde-ba27-069a5101ce30`
(EXTENDS `c072de94-f87f-48e3-a8e1-6a6a4d4f5a89`). Read that before starting — it records
the agreed cuts and the safety rationale.

## Goal

Durable, reused tooling for **general sequence-email authoring across multiple drip
campaigns (sequences)** — reviewed, production-safe bulk field remediation. First real run:
the 22-row first-name personalization job (20 subject-only, 2 HTML-body greeting).

## What already exists (reuse, do not reinvent)

The single-email `kit sequence email update` command already implements the entire per-row
safety spine in `src/KitCLI/Commands/SequenceCommands.cs`:

- `HandleEmailUpdate` (line ~235): `--subject` XOR `--content-file`; `--expect-subject` and
  `--expect-content-sha256` concurrency guards; pre-read, one-field PUT, post-read verify,
  invariant comparison; `Sha256Hex` (line ~679); an update-report struct + printer.
- API client: `UpdateSequenceEmailAsync` / `GetSequenceEmailAsync`
  (`Services/KitApiClient.cs`), `SequenceEmailUpdateRequest` model.

**`update-batch` must call this same per-row update-and-verify path.** It is manifest parse
+ loop + aggregate, NOT a new mutation path.

## Safety spine (invariant — enforced structurally, not by flags)

- One field per PUT (`subject` XOR `content`); the batch request body can never contain
  `published`, `position`, `delay_value`, `delay_unit`, `send_days`, `email_template_id`,
  `preview_text`, or sender/from.
- Per-row guard vs live state: `expectSubject` (subject rows) / `expectContentSha256`
  (content rows). Mismatch = hard exception, zero writes.
- Dry-run default; writes require `--apply`.
- Per-email read-back after each write; any non-target diff (esp. `published`/`position`) is
  a hard exception.
- `--stop-on-error` default; no auto-rollback.
- Lifecycle (publish/unpublish/reorder) is OUT of this command — separate future commands
  only.

## Explicitly CUT (see spec for rationale)

- No `--confirm-manifest-sha256` **gate** — manifest hash is recorded as provenance in the
  report only.
- No separate `--confirm-field-scope` required flag (fold into `--apply` or drop).
- No per-row whole-sequence `(position, emailId)` re-list/diff — per-email read-back suffices.

## Tasks

### 1. Manifest model + strict parser
- [ ] `Models/BatchManifest.cs`: `schemaVersion` (==1), `name`, `source`, `items[]`
  (`sequenceId`, `expectedSequenceName`, `emailId`, `field` ∈ {subject,content},
  `expectSubject`|`contentFile`+`expectContentSha256`, `replacement`, `expectedPublished`,
  `expectedPosition`). Add to `KitJsonContext` (AOT — no reflection).
- [ ] Strict validation: known schemaVersion; reject unknown keys (no scope-broadening);
  exactly one target field per row; no duplicate `(sequenceId, emailId)`.

### 2. `kit sequence email update-batch <manifest.json>` command
- [ ] Route in `Program.cs`; `HandleEmailUpdateBatch` in `SequenceCommands.cs`.
- [ ] Flags: `--apply` (default dry-run), `--stop-on-error` (default), `--format text|json`,
  `--report <path>`, `--skip-verified` (resume).
- [ ] Preflight: parse+validate; per row read sequence (verify `expectedSequenceName`), read
  email (verify parent `sequenceId`, `published`/`position` vs expectations, guard match);
  any mismatch → zero writes.
- [ ] Dry-run summary grouped by sequence (e.g. "Bootcamp 2.0: 8 subject, 1 body"); old →
  intended per row (body shown as file path + hash, never raw HTML). Exit without `--apply`.
- [ ] Apply: per row, reuse the single-email update-and-verify path; stop-on-error; aggregate.

### 3. Resumability
- [ ] `--skip-verified` consumes a prior `--report` (or emitted state) so a failure at row N
  doesn't force re-running verified rows.

### 4. Audit report
- [ ] Reuse/extend the existing update-report shape → batch report: manifest hash
  (provenance), tool version, timestamps, account-profile name (never API key), per-item
  pre-state + target result + invariant result, final counts
  (preflighted/updated/verified/skipped/failed), and the explicit "no sends/enrollment/tags/
  scheduling/publish/position changes requested or observed" statement.

### 5. Manifest generate helper (IN 1.7.1)
- [ ] Command that reads specified sequence IDs and EMITS a candidate manifest with each
  email's current `subject`/content-file + guards (`expectSubject`/`expectContentSha256`)
  pre-filled from live state, for Aaron to review/trim before apply.
- [ ] Decide command name (`kit sequence email generate-manifest <seq-id...>` or similar) and
  how content bodies are emitted to reviewable `contentFile`s.

### 6. Tests
- [ ] Manifest parse/validate unit tests (bad schema, dup rows, unknown keys, multi-field).
- [ ] Command tests via `MockKitServer`: dry-run makes zero writes; guard mismatch aborts
  whole batch; stop-on-error halts after failing row; apply sends exactly one field/row;
  read-back mismatch is a hard error; resume skips verified rows.
- [ ] Follow existing patterns in `src/KitCLI.Tests/Commands/SequenceEmailCommandTests.cs`.

### 7. AOT + release
- [ ] `dotnet publish -c Release /p:PublishAot=true` — zero new AOT warnings.
- [ ] Bump `Directory.Build.props` `1.7.0` → `1.7.1`; update `RELEASE_NOTES.md`.

## Validation before any production write (from spec)

Regression lab: seq `2874159` (TEST ONLY), fixtures `10240575` (pos 0), `10240576` (pos 1),
both unpublished. Baseline → dry-run → apply subject-only on 10240575 → verify only subject
changed / IDs+positions identical → restore → repeat content-only on 10240576 with SHA guard
+ `--content-file` → restore. Nothing published/enrolled/sent. Only then: generate the real
22-row manifest, Aaron reviews, dry-run all rows (zero writes, all exceptions resolved), one
explicit batch apply.

## Downstream — AFTER 1.7.1 ships (not part of the build)

Update `petabridge-skills/skills/kit-cli` (`SKILL.md` + `references/commands.md`, currently
v1.5.0) against the **shipped 1.7.1 binary** — new commands, manifest schema, worked
dry-run→apply example, and the "never touches publish/position/scheduling" contract. The
skill's own rule is to verify against the real command surface, not `--help`.
