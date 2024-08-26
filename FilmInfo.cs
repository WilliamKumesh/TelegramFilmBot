namespace WebSiteParser
{
    public class FilmInfo
    {
        public string Url { get; set; }
        public string Name { get; set; }
        public string Year { get; set; }
        public string Description { get; set; }
        public List<string> Genres { get; set; }
        public string Rate { get; set; }

        public FilmInfo()
        {

        }
    }
}
