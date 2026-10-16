using System;
using System.Net;
using System.Text.Json;

namespace GameClient.Networking
{
    public sealed class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        private ApiException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public static ApiException From(HttpStatusCode statusCode, string content, JsonSerializerOptions json)
        {
            var message = $"Server error ({(int)statusCode})";

            try
            {
                var problem = JsonSerializer.Deserialize<ProblemDetailsDto>(content, json);
                if (!string.IsNullOrEmpty(problem?.Title))
                    message = problem.Title;
            }
            catch (JsonException)
            {
                // the response body is not ProblemDetails, keep the generic message
            }

            return new ApiException(statusCode, message);
        }
    }
}