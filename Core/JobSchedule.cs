namespace JobRunner.Core
{
    public class JobSchedule
    {
        public required string JobName { get; set; }
        public TimeSpan Interval { get; set; }
        public required Dictionary<string, string> Parameters { get; set; }
    }
}