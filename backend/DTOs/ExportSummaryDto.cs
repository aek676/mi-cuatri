namespace backend.Dtos
{
    public class ExportSummaryDto
    {
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Failed { get; set; }
        public List<string>? Errors { get; set; }
    }
}
