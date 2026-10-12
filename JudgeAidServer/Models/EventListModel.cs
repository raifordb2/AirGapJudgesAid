namespace JudgesAid.Models
{
    using System.Text.Json.Serialization;

    public class EventListModel
    {
        [JsonPropertyName("event_list")]
        public List<EventItemModel> EventList { get; set; } = new();
    }

    public class EventItemModel
    {
        [JsonPropertyName("event_id")]
        public string EventId { get; set; } = string.Empty;

        [JsonPropertyName("event_name")]
        public string EventName { get; set; } = string.Empty;

        [JsonPropertyName("event_date")]
        public string EventDate { get; set; } = string.Empty;

        public override string ToString()
        {
            return EventName;
        }
    }
}