# GraceS3 File Endpoints Implementation

This repository now includes file-specific endpoints that mirror the bucket endpoint structure.

## Created Files

- **GraceS3/Files/Endpoints/EndpointGroup.cs** - Groups all file endpoint mappings using `EndpointGroup`.
- **GraceS3/Files/Endpoints/** - Individual endpoint classes:
  - `CreateFileEndpoint` (POST /files/create)
  - `GetFileOneEndpoint` (GET /files/{id})  
  - `UpdateFileEndpoint` (PUT /files/{id}/update)
  - `DeleteFileEndpoint` (DELETE /files/{id})
  - `GetFilesListEndpoint` (GET /files/list)

## Structure

Each endpoint class follows the same pattern as bucket endpoints:
```csharp
public static void Map(IEndpointRouteBuilder builder) {
    var group = new EndpointGroup();
    group.<EndpointName>Endpoint.Map(group);
}
```

The implementation mirrors bucket operations with file paths: `/bucket/{id}/files{...}`.

## Notes

- Uses `GraceS3.Files.Operations` for future logic (currently placeholder).
- Directory structure organized under `GraceS3/Files/Endpoints`.
- No changes to main application code required; endpoint registration handled internally.