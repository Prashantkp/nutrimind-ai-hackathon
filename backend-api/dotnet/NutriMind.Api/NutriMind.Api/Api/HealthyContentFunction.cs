using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NutriMind.Api.Models;
using NutriMind.Api.Services;

namespace NutriMind.Api.Api;

public class HealthyContentFunction
{
    private readonly ILogger<HealthyContentFunction> _logger;
    private readonly IOpenAIService _openAiService;

    public HealthyContentFunction(IOpenAIService openAiService, ILogger<HealthyContentFunction> logger)
    {
        _logger = logger;
        _openAiService = openAiService;
    }

    [Function("HealthyContentFunction")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post",Route = "healthyContent")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Starting healthy content generation");
            var response = await _openAiService.ProvideHealthyTip("I need a healthy eating tip.");
            return await CreateSuccessResponse(req, response, "Healthy tip generated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting healthy content");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
        }
        
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