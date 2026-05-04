using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NutriMind.Api.Models;
using NutriMind.Api.Services;
using System.Net;

namespace NutriMind.Api.Api
{
    public class AdminFunction
    {
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        private readonly ILogger<AdminFunction> _logger;
        private readonly IAuthService _authService;

        public AdminFunction(ILogger<AdminFunction> logger, IAuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        [Function("UploadRecipesExcel")]
        public async Task<HttpResponseData> UploadRecipesExcel(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/upload-recipes")] HttpRequestData req)
        {
            _logger.LogInformation("Processing Excel recipe upload request");

            try
            {
                // Verify the user is authenticated
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Verify the user has admin privileges
                if (!_authService.IsAdminFromRequest(req))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Forbidden, "Admin privileges required");
                }

                // Check Content-Length header first for an early size rejection
                if (req.Headers.TryGetValues("Content-Length", out var contentLengthValues))
                {
                    var contentLengthStr = contentLengthValues.FirstOrDefault();
                    if (long.TryParse(contentLengthStr, out var contentLength) && contentLength > MaxFileSizeBytes)
                    {
                        _logger.LogWarning("Upload rejected: Content-Length {ContentLength} exceeds limit of {MaxSize} bytes", contentLength, MaxFileSizeBytes);
                        return await CreateErrorResponse(req, HttpStatusCode.RequestEntityTooLarge,
                            "File too large. Maximum allowed size is 10 MB.");
                    }
                }

                // Read the body and enforce the size limit
                using var memoryStream = new MemoryStream();
                var buffer = new byte[81920]; // 80 KB read buffer
                int bytesRead;
                long totalBytesRead = 0;

                while ((bytesRead = await req.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    totalBytesRead += bytesRead;
                    if (totalBytesRead > MaxFileSizeBytes)
                    {
                        _logger.LogWarning("Upload rejected: body size exceeded limit of {MaxSize} bytes", MaxFileSizeBytes);
                        return await CreateErrorResponse(req, HttpStatusCode.RequestEntityTooLarge,
                            "File too large. Maximum allowed size is 10 MB.");
                    }
                    memoryStream.Write(buffer, 0, bytesRead);
                }

                memoryStream.Seek(0, SeekOrigin.Begin);

                // Validate that the uploaded file is an Excel workbook by checking its magic bytes.
                // .xlsx files are ZIP archives (PK header: 50 4B 03 04).
                // .xls files use the Compound Document File format (D0 CF 11 E0 header).
                var fileBytes = memoryStream.ToArray();
                if (!IsExcelFile(fileBytes))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest,
                        "Invalid file format. Only Excel files (.xlsx or .xls) are accepted.");
                }

                _logger.LogInformation("Admin {UserId} uploaded an Excel file ({Size} bytes)", userId, totalBytesRead);

                var result = new
                {
                    message = "File uploaded successfully",
                    fileSizeBytes = totalBytesRead,
                    uploadedBy = userId,
                    uploadedAt = DateTime.UtcNow
                };

                return await CreateSuccessResponse(req, result, "Excel file uploaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Excel recipe upload");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        /// <summary>
        /// Checks whether <paramref name="data"/> starts with a known Excel magic-byte sequence.
        /// </summary>
        private static bool IsExcelFile(byte[] data)
        {
            if (data.Length < 4)
                return false;

            // .xlsx — ZIP/OOXML: PK\x03\x04
            if (data[0] == 0x50 && data[1] == 0x4B && data[2] == 0x03 && data[3] == 0x04)
                return true;

            // .xls — Compound Document File Format: D0 CF 11 E0
            if (data.Length >= 8 &&
                data[0] == 0xD0 && data[1] == 0xCF && data[2] == 0x11 && data[3] == 0xE0)
                return true;

            return false;
        }

        private async Task<HttpResponseData> CreateSuccessResponse<T>(HttpRequestData req, T data, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ApiResponse<T>.SuccessResponse(data, message));
            return response;
        }

        private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
        {
            var response = req.CreateResponse(statusCode);
            await response.WriteAsJsonAsync(ApiResponse<object>.ErrorResponse(message));
            return response;
        }
    }
}
