namespace JobRunner.Core
{
    public interface IJobTask
    {
        string Name { get; }
        Task ExecuteAsync(JobContext context, CancellationToken cancellationToken);
    }
}