namespace ProActive2508.Models.Lisa
{
    public class Antwort
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public int ProjectId { get; set; }
        public int Rating { get; set; } // 1–5
        public DateTime Date { get; set; }

    }
}
