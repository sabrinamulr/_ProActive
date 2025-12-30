namespace ProActive2508.Models.Lisa
{
    public class ProjektUmfrage
    {
        public int Id { get; set; } // DB später
        public string Name { get; set; }
        public List<Frage> Questions { get; set; } = new();

    }
}
