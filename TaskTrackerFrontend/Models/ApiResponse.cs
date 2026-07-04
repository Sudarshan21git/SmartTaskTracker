using System.Text.Json.Serialization;

namespace TaskTrackerFrontend.Models
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        // Note: Your backend doesn't include these in TaskController, but AuthController does
        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }
    }

    // Non-generic version for simple responses
    public class ApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }
    }
}