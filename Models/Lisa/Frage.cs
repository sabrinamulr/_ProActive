namespace ProActive2508.Models.Lisa
{
    public class Frage
    {
        public int Id { get; set; } // DB später
        public string Text { get; set; } = string.Empty;
        public int CategoryId { get; set; } // FK für SQL

    }
}
