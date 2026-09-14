namespace THMS.Logic.ViewModels
{
    public static class ImportProgressReporter
    {
        public static void Report(
            IProgress<ImportProgress>? progress,
            int completed,
            int total,
            string? phase = null,
            int stride = 25)
        {
            if (progress is null)
                return;
            if (phase is null && completed != 0 && completed != total && stride > 0 && completed % stride != 0)
                return;

            progress.Report(new ImportProgress(completed, total, phase));
        }
    }
}
