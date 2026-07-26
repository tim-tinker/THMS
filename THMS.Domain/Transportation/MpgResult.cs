namespace THMS.Domain.Transportation
{
    public class MpgResult
    {
        public DateTime Date { get; set; }
        public decimal MilesDriven { get; set; }
        public decimal GallonsUsed { get; set; }
        public decimal Mpg => GallonsUsed > 0 ? MilesDriven / GallonsUsed : 0;
    }
}
