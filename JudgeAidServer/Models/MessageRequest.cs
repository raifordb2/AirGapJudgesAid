namespace JudgesAid.Models
{
    public class MessageRequest
    {
        public string ClientName { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }

    public class MessageResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public DateTime ServerTime { get; set; }
    }
}
