# File storage (Batch 4d)

Documents use `IFileStorageService`. Two implementations:

| Provider | Class | When |
|---|---|---|
| `Local` (default) | `LocalFileStorageService` | Local disk under `LocalStorage/` — default for this repo |
| `AzureBlob` | `AzureBlobFileStorageService` | Azure Blob API — use **Azurite** locally, real Azure in deploy |

> **Current state:** Azure Blob support is built and tested but not currently in use — the project runs on local disk storage until a real deployment environment is set up. To test the Azure path locally, run Azurite and set `FileStorage:Provider` to `AzureBlob`; see the Azurite command below.

## Local (default)

```json
"FileStorage": {
  "Provider": "Local",
  "LocalRootPath": "LocalStorage"
}
```

No extra process required. Existing documents keep working.

## Azure Blob via Azurite (local emulator)

1. Start Azurite (one-time per terminal):

```bash
# From repo root (local install under tools/azurite):
node tools/azurite/node_modules/azurite/dist/src/azurite.js --silent --location ./.azurite --blobHost 127.0.0.1 --blobPort 10000
```

2. Switch config (e.g. in `appsettings.Development.json` or user-secrets):

```json
"FileStorage": {
  "Provider": "AzureBlob",
  "AzureBlob": {
    "ConnectionString": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;",
    "ContainerName": "vendor-documents"
  }
}
```

The connection string above is Microsoft’s **well-known Azurite account** — safe to commit for local/dev. Production should use Key Vault / env vars with a real storage account.

3. Restart the API. Container `vendor-documents` is created on startup.

## Existing documents when switching providers

Storage paths in the DB are relative (`yyyy-MM/{guid}.ext`) and are **provider-specific locations**. Switching `Local` → `AzureBlob` does **not** auto-migrate files.

- **New uploads** go to the active provider.
- **Old Local files** remain under `LocalStorage/`. Downloads will 404 until you either:
  - Switch back to `Provider: Local`, or
  - Manually copy files into the blob container using the same relative path as the blob name (e.g. Azure Storage Explorer / AzCopy against Azurite).

Automatic bulk migration is out of scope for 4d.

## Real Azure later

Replace `AzureBlob:ConnectionString` with a real storage account connection string (or managed identity). No code change — same `AzureBlobFileStorageService`.
