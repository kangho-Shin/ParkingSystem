# Image Pipeline Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upload new-system LPR images through ImageUploadAgent and display them reliably in EdgeManager through ParkImageServer.

**Architecture:** LPR writes a completed image into a configured local watch folder. ImageUploadAgent checks existence, uploads multipart data, and deletes only confirmed files; consumers download by file name from the site ImageServer URL stored by EdgeService.

**Tech Stack:** .NET 9, ASP.NET Core, WinForms, HttpClient, xUnit

**Spec:** `docs/superpowers/specs/2026-09-20-local-configuration-image-server-design.md`

## Global Constraints

- Only the new file name format is supported.
- Format: `{sitenum}_{groupnum}_{devicenum}_{laneid}_{Entry|Exit}_{yyyyMMddHHmmssfff}_{차량번호}_{EventId}.jpg`.
- API routes remain `/api/image/upload`, `/api/image/exists`, and `/api/image/download`.
- Database events store file names, never absolute paths.
- Upload failures retain the original local file.

## Review Focus

- Vehicle numbers containing Korean text must survive URL encoding and multipart upload unchanged.
- Partial image writes must not be uploaded.
- Invalid names and path traversal must be rejected before file access.
- Image server outage must leave the pending local file intact.
- A slow previous download must not replace the image for a newer row selection.

---

### Task 1: New image file-name parser and server path safety

**Files:**
- Create: `ImageServer/ParkImageServer/ImageFileName.cs`
- Modify: `ImageServer/ParkImageServer/Repositories/ImageRepository.cs`
- Create: `tests/ParkImageServer.Tests/ParkImageServer.Tests.csproj`
- Create: `tests/ParkImageServer.Tests/ImageFileNameTests.cs`
- Create: `tests/ParkImageServer.Tests/ImageRepositoryTests.cs`

**Interfaces:**
- Produces: `ImageFileName.TryParse(string, out ImageFileName?)`, `CaptureAt`, `FileName`

- [ ] **Step 1: Write failing parser tests**

Test the approved Entry and Exit examples, Korean vehicle number, invalid timestamp, wrong token count, absolute path, `..`, and directory separators.

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test tests/ParkImageServer.Tests/ParkImageServer.Tests.csproj`

Expected: FAIL because `ImageFileName` does not exist.

- [ ] **Step 3: Implement strict parsing**

Split on `_`, require eight tokens, require `Entry` or `Exit`, parse the sixth token with exact `yyyyMMddHHmmssfff`, validate the eighth GUID token after removing the extension, and retain the original safe file name.

- [ ] **Step 4: Replace fixed substring path extraction**

Build storage path only as `RootPath/yyyy/MM/dd/FileName`. Both save and lookup must call the same parser.

- [ ] **Step 5: Run tests and commit**

```bat
dotnet test tests\ParkImageServer.Tests\ParkImageServer.Tests.csproj
git add ImageServer\ParkImageServer tests\ParkImageServer.Tests
git commit -m "fix: support new parking image names"
```

### Task 2: Harden upload, exists, and download API behavior

**Files:**
- Modify: `ImageServer/ParkImageServer/Controllers/ImageController.cs`
- Modify: `ImageServer/ParkImageServer/Services/ImageService.cs`
- Create: `tests/ParkImageServer.Tests/ImageEndpointTests.cs`

**Interfaces:**
- Consumes: `ImageFileName.TryParse`
- Produces: unchanged public ImageServer routes with deterministic status codes

- [ ] **Step 1: Write failing API tests**

Assert valid upload/download bytes, duplicate exists response, invalid name `400`, missing file `404`, and zero-length upload `400`.

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test tests/ParkImageServer.Tests/ParkImageServer.Tests.csproj --filter FullyQualifiedName~ImageEndpointTests`

Expected: at least invalid-name and empty-upload cases fail.

- [ ] **Step 3: Implement validation and asynchronous streaming**

Validate before repository access, use async file copy, preserve the existing 5-second completed-file wait on download, and return no physical server path in the public upload response.

- [ ] **Step 4: Run the server suite and commit**

```bat
dotnet test tests\ParkImageServer.Tests\ParkImageServer.Tests.csproj
git add ImageServer\ParkImageServer tests\ParkImageServer.Tests
git commit -m "fix: validate image server requests"
```

### Task 3: Configure ImageUploadAgent from EdgeService

**Files:**
- Create: `ImageServer/ImageUploadAgent/EdgeConfigurationClient.cs`
- Create: `ImageServer/ImageUploadAgent/ImageUploadClient.cs`
- Modify: `ImageServer/ImageUploadAgent/MainForm.cs`
- Modify: `ImageServer/ImageUploadAgent/App.config`
- Create: `tests/ImageUploadAgent.Tests/ImageUploadAgent.Tests.csproj`
- Create: `tests/ImageUploadAgent.Tests/ImageUploadClientTests.cs`

**Interfaces:**
- Consumes: EdgeService local setup/config API, ImageServer API
- Produces: reusable `ExistsAsync` and `UploadAsync` methods

- [ ] **Step 1: Write failing upload behavior tests**

Test existing remote file deletes local file, successful upload deletes local file, HTTP/API failure retains it, and a file modified within two seconds is skipped.

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test tests/ImageUploadAgent.Tests/ImageUploadAgent.Tests.csproj`

Expected: FAIL because clients are not separated from `MainForm`.

- [ ] **Step 3: Extract clients and load site settings**

Keep only `EdgeServiceBaseUrl` in `App.config`. Read `ImageServerUrl` and watch-folder settings from EdgeService. Reuse one `HttpClient`; send multipart fields named exactly `file` and `fileName`.

- [ ] **Step 4: Keep retry-safe deletion rules**

Delete only after `Exists=true` or upload response `Result=0`. Keep the file for exceptions, cancellation, non-success HTTP, malformed JSON, and API failure.

- [ ] **Step 5: Run tests and commit**

```bat
dotnet test tests\ImageUploadAgent.Tests\ImageUploadAgent.Tests.csproj
git add ImageServer\ImageUploadAgent tests\ImageUploadAgent.Tests
git commit -m "feat: load image upload settings from EdgeService"
```

### Task 4: Download and display images directly from ParkImageServer

**Files:**
- Create: `src/Edge/Parking.EdgeManager.Core/ImageServerClient.cs`
- Modify: `src/Edge/Parking.EdgeManager.Core/IEdgeManagementClient.cs`
- Modify: `src/Edge/Parking.EdgeManager.Core/EdgeManagementClient.cs`
- Modify: `src/Edge/Parking.EdgeManager.Core/EdgeManagerPresenter.cs`
- Modify: `src/Edge/Parking.EdgeManager/MainForm.cs`
- Modify: `src/Edge/Parking.EdgeManager/Program.cs`
- Modify: `tests/Parking.EdgeManager.Tests/EdgeManagerPresenterTests.cs`
- Create: `tests/Parking.EdgeManager.Tests/ImageServerClientTests.cs`

**Interfaces:**
- Consumes: local setup `ImageServerUrl`, event `InImage`/`OutImage`
- Produces: `ImageServerClient.DownloadAsync(string?, CancellationToken)`

- [ ] **Step 1: Write failing client tests**

Test Korean file-name escaping, `404` returning `null`, success returning exact bytes, and empty name avoiding HTTP.

- [ ] **Step 2: Write the stale-selection failing test**

Delay the first selection download, complete the second selection first, then complete the first. Assert the view still shows the second selection.

- [ ] **Step 3: Run tests and verify RED**

Run: `dotnet test tests/Parking.EdgeManager.Tests/Parking.EdgeManager.Tests.cs`

Expected: FAIL because image download still targets EdgeService and stale requests are not guarded.

- [ ] **Step 4: Implement ImageServer client and selection cancellation**

Build `api/image/download?fileName=` with `Uri.EscapeDataString`. The presenter owns a selection `CancellationTokenSource`; each new selection cancels and disposes the previous source. Only the current request calls `ShowImages`.

- [ ] **Step 5: Remove obsolete EdgeService physical-image endpoint**

After all callers use `ImageServerClient`, remove `ManagementController.GetImage` and `Edge:ImageDirectory`. Keep image-server URL in local setup, not EdgeManager `appsettings.json`.

- [ ] **Step 6: Run tests and commit**

```bat
dotnet test tests\Parking.EdgeManager.Tests\Parking.EdgeManager.Tests.csproj
git add src\Edge\Parking.EdgeManager.Core src\Edge\Parking.EdgeManager src\Edge\Parking.EdgeService tests\Parking.EdgeManager.Tests
git commit -m "feat: display images from ParkImageServer"
```

### Task 5: End-to-end image verification

**Files:**
- Modify: `start-ldm-test.bat` or create a focused sequential image-test batch beside existing launchers
- Modify: `docs/04-development-status.md`

**Interfaces:**
- Consumes: Tasks 1-4 and completed local configuration foundation
- Produces: repeatable field test sequence

- [ ] **Step 1: Create one sequential test batch**

Set required connection strings and URLs, start ParkImageServer, Central API, Gateway, EdgeService, ImageUploadAgent, and EdgeManager in order, with `pause` between each program.

- [ ] **Step 2: Execute one Entry image test**

Place a valid Entry image in the watched folder. Confirm upload, local deletion, `yyyy/MM/dd` server storage, and EdgeManager entry display.

- [ ] **Step 3: Execute one Exit image test**

Place a valid Exit image and trigger the matching exit event. Confirm entry and exit pictures display together.

- [ ] **Step 4: Execute the outage retry test**

Stop ParkImageServer, add an image, confirm it remains locally; restart the server and confirm automatic upload and deletion.

- [ ] **Step 5: Run affected and solution tests**

Run: `dotnet test tests/ParkImageServer.Tests/ParkImageServer.Tests.csproj`

Run: `dotnet test tests/ImageUploadAgent.Tests/ImageUploadAgent.Tests.csproj`

Run: `dotnet test tests/Parking.EdgeManager.Tests/Parking.EdgeManager.Tests.csproj`

Run: `dotnet test ParkingSystem.sln`

Expected: PASS.

- [ ] **Step 6: Record successful integration and commit**

```bat
git add docs\04-development-status.md *.bat
git commit -m "test: verify image pipeline integration"
```
