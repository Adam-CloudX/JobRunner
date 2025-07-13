namespace JobRunner.Models
{
    public class JobSchedule
    {
        public required string JobName { get; set; }
        public TimeSpan Interval { get; set; }
        public required Dictionary<string, string> Parameters { get; set; }
        public bool Enabled { get; set; } = true;
        public bool IsPreview { get; set; } = false;
    }
}