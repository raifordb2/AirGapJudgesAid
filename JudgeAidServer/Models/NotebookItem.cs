namespace JudgeAidServer.Models
{
    using System.Text.Json.Serialization;
    public class NotebookItemModel
    {
        [JsonPropertyName("notebook_id")]
        public Guid NotebookId { get; set; } = Guid.Empty;

        [JsonPropertyName("team_number")]
        public string TeamNumber { get; set; } = string.Empty;

        [JsonPropertyName("Last_updated")]
        public DateTime LastUpdated { get; set; } = DateTime.MinValue;
        [JsonPropertyName("link")]
        public string Link { get; set; } = string.Empty;
    }
    public class NotebookListModel
    {
        [JsonPropertyName("grade_level")]
        public GradeLevel EventGradeLevel { get; set; } = GradeLevel.Elementary;

        [JsonPropertyName("notebook_list")]
        public List<NotebookItemModel> NotebookList { get; set; } = new();
    }

    public enum GradeLevel
    {
        Elementary,
        Middle,
        High,
        University
    }
}
