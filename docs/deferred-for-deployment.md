# Deferred for deployment

Items that are fine for local/dev but must be decided before a real company
deployment. Capability is already in the codebase — this is configuration /
licensing, not missing features.

## MediatR license (v14)

MediatR is on **v14**, which requires a commercial license for production use
(free for development and testing). Before any real deployment, either:

- obtain a Lucky Penny / MediatR commercial license, or
- downgrade to MediatR **v12.x** (last line without that production license requirement)

Until then, the startup warning about a missing license key is expected in local
dev and can be ignored.

## File storage provider

File storage uses `FileStorage:Provider` = **`Local`** (checked-in default in
`appsettings.json` / `appsettings.Development.json`).

`AzureBlobFileStorageService` is built and covered by tests (against Azurite when
running). It is **unused** day-to-day. When a real deployment environment exists
(Azure Blob or compatible on-prem), switch:

```json
"FileStorage": {
  "Provider": "AzureBlob"
}
```

See `docs/file-storage.md` for Azurite local testing and the migration caveat
(existing Local files are not auto-copied to blob).
