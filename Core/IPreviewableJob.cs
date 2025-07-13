namespace JobRunner.Core
{
    public interface IPreviewableJob : IJobTask
    {
        Task PreviewAsync(JobContext context, CancellationToken cancellationToken);
    }
}