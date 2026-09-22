# FileManagerApi

FileManagerApi is a small ASP.NET Core Web API for uploading, storing, and retrieving files. It accepts single files, batches of files, and images through `multipart/form-data` endpoints, validates and stores them safely on disk under randomized names, tracks their metadata in a SQL Server database via Entity Framework Core, and serves them back either as a direct download or as a range-enabled stream.

## Features

- Single file upload (`POST /Files`)
- Multiple file upload in one request, capped at 10 files (`POST /Files/upload-files`)
- Dedicated image upload endpoint with image-specific validation (`POST /Files/upload-image`)
- File download with `Content-Disposition: attachment` (`GET /Files/download/{id}`)
- File streaming with HTTP Range support for partial/resumable downloads (`GET /Files/stream/{id}`)
- Server-generated, non-guessable stored file names (client-supplied names are never used as a disk path)
- File size validation (rejects empty and oversized files)
- File name validation (blocks path separators, `.`/`..`, control characters, and invalid filesystem characters)
- File signature ("magic number") validation:
  - A denylist blocking known dangerous binary signatures (Windows executables, OLE compound files, ELF binaries, shell scripts) for generic uploads
  - A true allowlist of image signatures (JPEG/PNG/GIF/BMP) for the image upload endpoint
- File metadata persisted to SQL Server (original name, content type, stored name, extension)
- Streaming download implementation with no in-memory buffering of file content

## Tech Stack

- **.NET 9 / ASP.NET Core Web API** (`Microsoft.NET.Sdk.Web`, `net9.0`, nullable reference types and implicit usings enabled)
- **Entity Framework Core 9** (`Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.Tools`) — code-first modeling and migrations
- **SQL Server** (`Microsoft.EntityFrameworkCore.SqlServer`) — the only configured database provider
- **FluentValidation** (`FluentValidation` + `FluentValidation.AspNetCore`) — request validation, wired into MVC via `AddFluentValidationAutoValidation()`
- **ASP.NET Core MVC controllers** (`AddControllers()` / `MapControllers()`) for the API surface
- **`MapStaticAssets()`** (.NET 9 static asset pipeline) for serving build-time `wwwroot` assets

> **Note:** This project does **not** currently include Swagger/OpenAPI (no `Swashbuckle`/`Microsoft.AspNetCore.OpenApi` package or `AddSwaggerGen`/`UseSwagger` calls are present). See [Getting Started](#getting-started) for how to exercise the API without it.

## Architecture

The project is a single ASP.NET Core Web API project (`FileManagerApi`) organized by responsibility:

| Folder / File | Responsibility |
|---|---|
| `Controllers/` | `FilesController` — the thin HTTP layer. Binds incoming multipart requests to `Contracts` records, calls `IFileService`, and maps the result to a `201 Created`, `200 OK`, or `404 Not Found` response. Contains no file-handling or validation logic itself. |
| `Contracts/` | Request DTOs (`UploadFileRequest`, `UploadImageRequest`, `UploadManyFilesRequest`) and their FluentValidation validators (`UploadFileRequestValidator`, `UploadImageRequestValidator`, `UploadManyFilesRequestValidator`). Two validators — `FileSizeValidator` and `LockedSignatureValidator` — are shared, reusable `AbstractValidator<IFormFile>` rules applied to every uploaded file. |
| `Services/` | `IFileService` / `FileService` — the only layer that touches the file system. Responsible for generating safe stored file names, writing/reading files with `FileStream`, and coordinating metadata persistence through `ApplicationDbContext`. |
| `Entities/` | `UploadedFiles` — the EF Core entity representing a stored file's metadata. |
| `Persistence/` | `ApplicationDbContext` (exposes `DbSet<UploadedFiles> Files`), `UploadedFileConfiguration` (Fluent API column configuration: max lengths for each string property), and `Migrations/` (the EF Core migration history for the `Files` table). |
| `Program.cs` | Composition root. Registers MVC controllers, the SQL Server `DbContext`, FluentValidation auto-validation and validators, `IFileService`, and configures the HTTP pipeline (`UseHttpsRedirection`, `UseAuthorization`, `MapControllers`, `MapStaticAssets`). |
| `wwwroot/Uploads/` | Physical storage location for all uploaded file content (both generic files and images), written under randomized names. |
| `wwwroot/Images/` | Holds pre-existing static assets bundled with the project; it is not used as a destination for new uploads. |

## File Upload Flow

1. A client sends a `multipart/form-data` `POST` request to `/Files`, `/Files/upload-files`, or `/Files/upload-image`.
2. ASP.NET Core model binding maps the form part(s) onto the corresponding `Contracts` record (`IFormFile` or `IFormFileCollection`).
3. Before the controller action runs, FluentValidation's auto-validation evaluates the matching validator:
   - Confirms a file (or at least one file, for the multi-upload endpoint) was actually provided.
   - **File size validation** — rejects 0-byte files and anything larger than 1 MB.
   - **File name validation / path traversal protection** — rejects file names containing `/`, `\`, control characters, any character in `Path.GetInvalidFileNameChars()`, or that are exactly `.` or `..`, so a file name can never be used to escape the intended storage directory.
   - **File signature ("magic number") validation** — reads the leading bytes of the file and rejects a denylist of dangerous signatures (Windows `MZ` executables/DLLs, OLE compound files, ELF binaries, `#!` shell scripts) for the generic upload endpoints. The image upload endpoint additionally checks the extension against an allowlist (`.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`) **and** verifies the actual bytes match a known JPEG/PNG/GIF/BMP signature, not just the file extension.
   - **Batch limits** — the multi-file endpoint rejects an empty file list and caps a single request at 10 files.
   - If any rule fails, the request is short-circuited with a `400 Bad Request` before the controller body executes.
4. On success, `FilesController` delegates to `FileService`, which:
   - Generates a **random stored file name** with `Path.GetRandomFileName()` — the original, client-supplied file name is never used as a file system path.
   - Ensures the `wwwroot/Uploads` directory exists.
   - Writes the file to disk with an asynchronous `FileStream` (`FileMode.CreateNew`, so an existing file is never silently overwritten) via `file.CopyToAsync(...)`.
   - Saves the file's **metadata** — original file name, content type, generated stored file name, and extension — as an `UploadedFiles` row through `ApplicationDbContext`.
5. The controller responds `201 Created`: single-file and image uploads return a `Location` header pointing at the file's download URL, and the multi-file upload returns the list of generated file IDs in the response body.

No antivirus or malware-scanning integration is implemented in this project.

## File Download Flow

1. `GET /Files/download/{id}` or `GET /Files/stream/{id}` is called with the file's `Guid` id.
2. `FileService` looks up the matching `UploadedFiles` row. If no row exists, or the physical file referenced by `StoredFileName` is missing from disk, it returns a `null` stream and the controller responds `404 Not Found`.
3. Otherwise, the file is opened directly as an asynchronous `FileStream` (`FileOptions.Asynchronous | FileOptions.SequentialScan`) and handed to `ControllerBase.File(Stream, contentType, fileName)`.
4. **No `MemoryStream` or `byte[]` buffering is used** — the stream is passed straight through to the HTTP response by ASP.NET Core's `FileStreamResult`, which also disposes the stream automatically once the response finishes. This keeps memory usage roughly constant regardless of file size, instead of loading the entire file into memory before sending it.
5. The response includes `Content-Disposition: attachment` with the original file name. `/stream/{id}` additionally enables range processing (`enableRangeProcessing: true`), so clients can send `Range` headers and receive `206 Partial Content` responses (useful for resumable downloads or media seeking).

## API Endpoints

Base route: `/Files`

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/Files` | Upload a single file |
| `POST` | `/Files/upload-files` | Upload multiple files (max 10) in one request |
| `POST` | `/Files/upload-image` | Upload a single image |
| `GET` | `/Files/download/{id}` | Download a file by id |
| `GET` | `/Files/stream/{id}` | Stream a file by id (supports HTTP `Range` requests) |

### `POST /Files`

- **Body:** `multipart/form-data` with one part named `File`
- **Validation:** required, ≤ 1 MB, safe file name, blocked-signature check
- **Response:** `201 Created` with a `Location` header pointing to `/Files/download/{id}`; empty body
- **Status codes:** `201 Created`, `400 Bad Request` (validation failure)

### `POST /Files/upload-files`

- **Body:** `multipart/form-data` with one or more parts named `Files` (max 10)
- **Validation:** at least one file, ≤ 10 files, each file ≤ 1 MB, safe file name, blocked-signature check
- **Response:** `201 Created` with a JSON array of the created file ids (`Guid[]`)
- **Status codes:** `201 Created`, `400 Bad Request` (validation failure)

### `POST /Files/upload-image`

- **Body:** `multipart/form-data` with one part named `Image`
- **Validation:** required, ≤ 1 MB, safe file name, blocked-signature check, extension must be one of `.jpg`/`.jpeg`/`.png`/`.gif`/`.bmp`, and the file content must match a real JPEG/PNG/GIF/BMP signature
- **Response:** `201 Created` with a `Location` header pointing to `/Files/download/{id}`; empty body
- **Status codes:** `201 Created`, `400 Bad Request` (validation failure)

### `GET /Files/download/{id}`

- **Route parameter:** `id` (`Guid`)
- **Response:** file content streamed with the stored content type and `Content-Disposition: attachment`
- **Status codes:** `200 OK`, `404 Not Found` (unknown id or missing file on disk)

### `GET /Files/stream/{id}`

- **Route parameter:** `id` (`Guid`)
- **Response:** file content streamed with the stored content type, `Content-Disposition: attachment`, and range support
- **Status codes:** `200 OK`, `206 Partial Content` (when a `Range` header is sent), `404 Not Found` (unknown id or missing file on disk)

## Validation & Security

**Implemented:**
- File size limits (rejects empty and > 1 MB files)
- File name checks that block path separators, `.`/`..`, control characters, and invalid filesystem characters — preventing a file name from being used for path traversal or writing outside the storage directory
- Files are always written to disk under a randomly generated name (`Path.GetRandomFileName()`); the original file name is stored only as metadata and is never used as a path
- A denylist of dangerous binary signatures (Windows executables/DLLs, OLE compound documents, ELF binaries, shell scripts) is checked on every upload
- The image upload endpoint additionally enforces an extension allowlist and verifies the actual file signature against known image formats (JPEG/PNG/GIF/BMP), not just the declared extension or content type
- Batch upload size is capped at 10 files per request

**Not implemented — be aware of these gaps:**
- No antivirus or malware scanning of uploaded content
- No content-type/signature allowlist for the generic upload endpoints (`/Files`, `/Files/upload-files`) — only the denylist above applies; arbitrary non-blocked file types are accepted
- No authentication or authorization — `Program.cs` calls `UseAuthorization()` but no authentication scheme or `[Authorize]` attributes are configured, so every endpoint is reachable by anyone who can reach the API
- No rate limiting or per-client upload quotas

## Database

The project uses **SQL Server** via EF Core, with a single `DbContext` (`ApplicationDbContext`) exposing one `DbSet<UploadedFiles> Files`, mapped to a `Files` table. Each row stores the metadata for one uploaded file:

| Column | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` | Primary key, generated with `Guid.CreateVersion7()` |
| `FileName` | `nvarchar(255)` | Original client-supplied file name |
| `StoredFileName` | `nvarchar(250)` | Randomly generated name used as the actual file name on disk |
| `ContentType` | `nvarchar(50)` | Content type supplied by the client at upload time |
| `FileExtension` | `nvarchar(10)` | Extension extracted from the original file name |

Schema history is managed with EF Core migrations under `FileManagerApi/Persistence/Migrations/` (currently `InitialCreate` and `UpdateUploadedFilesClass`).

## Configuration

All configuration lives in `FileManagerApi/appsettings.json` and `FileManagerApi/appsettings.Development.json`.

`appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_CONNECTION_STRING"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- **`ConnectionStrings:DefaultConnection`** — required. `Program.cs` throws an `InvalidOperationException` at startup if this value is missing or empty. It must point to a reachable SQL Server instance (LocalDB, a container, or a full SQL Server instance).
- **`Logging` / `AllowedHosts`** — standard ASP.NET Core logging and host-filtering configuration; `appsettings.Development.json` only overrides the `Logging` section for the Development environment.
- **File storage path** — not configurable via `appsettings`; `FileService` writes uploads to `wwwroot/Uploads` (relative to the application's web root) and creates that directory automatically if it doesn't exist.
- **Environment** — `Properties/launchSettings.json` sets `ASPNETCORE_ENVIRONMENT=Development` and defines two local profiles: `http` (`http://localhost:5181`) and `https` (`https://localhost:7030;http://localhost:5181`).

## Getting Started

### 1. Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- A reachable SQL Server instance (LocalDB, Docker container, or full SQL Server)
- The EF Core CLI tool, for creating/applying migrations:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

### 2. Clone the repository

```bash
git clone YOUR_REPOSITORY_URL
cd FileManager
```

### 3. Configure settings

Edit `FileManagerApi/appsettings.json` (or use an `appsettings.Development.json` override, user secrets, or an environment variable) and set a real connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "YOUR_CONNECTION_STRING"
}
```

### 4. Restore packages

```bash
dotnet restore
```

### 5. Apply EF Core migrations

```bash
dotnet ef database update --project FileManagerApi
```

### 6. Run the project

```bash
dotnet run --project FileManagerApi
```

The API will start on the URLs configured in `Properties/launchSettings.json` (`http://localhost:5181` and, for the `https` profile, `https://localhost:7030`).

### 7. Try the API

This project does not currently expose a Swagger/OpenAPI UI. Use the included `FileManagerApi/FileManagerApi.http` file with an HTTP client (e.g. the REST Client extension or Visual Studio's built-in `.http` support), or use `curl`/Postman as shown below.

## Example Requests

Replace the host/port with the profile you're running (`http://localhost:5181` by default).

**Upload a single file:**

```bash
curl -X POST http://localhost:5181/Files \
  -F "File=@/path/to/document.pdf"
```

**Upload multiple files:**

```bash
curl -X POST http://localhost:5181/Files/upload-files \
  -F "Files=@/path/to/file1.txt" \
  -F "Files=@/path/to/file2.txt"
```

**Upload an image:**

```bash
curl -X POST http://localhost:5181/Files/upload-image \
  -F "Image=@/path/to/photo.jpg"
```

**Download a file:**

```bash
curl -OJ http://localhost:5181/Files/download/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Stream a file with a range request:**

```bash
curl -H "Range: bytes=0-1023" \
  http://localhost:5181/Files/stream/3fa85f64-5717-4562-b3fc-2c963f66afa6
```
