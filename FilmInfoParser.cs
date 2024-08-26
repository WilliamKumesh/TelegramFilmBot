using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace WebSiteParser
{
    public static class FilmInfoParser
    {
        private static readonly string basicUrl = $"https://www.kinoafisha.info/search/?q=";
        private static string datePattern = @"(?<day>\d{1,2})\s+(?<month>\w+)\s+(?<year>\d{4})";
        private static string searchContent = $"//meta[@name='Description']";
        private static string searchLinks = $"//a[@class='shortList_ref']";
        private static string searchInfos = $"//span[@class='shortList_info']";
        private static string searchDescription = $"//div[@class='visualEditorInsertion filmDesc_editor more_content']";
        private static string rateName = "Рейтинг";
        private static string genreName = "Жанр";

        public static async Task<FilmInfo> GetFilmInfo(string name, int _year = 0)
        {
            string url = await FindFilmUrl(name, _year);

            if (url == null) 
            {
                throw new Exception("No such name");
            }

            HttpClient client = new HttpClient();

            string html = await client.GetStringAsync(url);

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            string content = document.DocumentNode.SelectSingleNode(searchContent).GetAttributeValue("content", "");

            string rating = ExtractRating(content);
            List<string> genres = ExtractGenres(content);
            string filmInfo = ExtractFilmInfo(document);
            string year = ExtractYear(content);

            FilmInfo film = new FilmInfo();

            film.Url = url;
            film.Name = name;
            film.Rate = rating;
            film.Genres = genres;
            film.Description = filmInfo;
            film.Year = year;

            return film;
        }

        public static async Task<string> GetTrailerLink(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(response);

                var linkNode = doc.DocumentNode.SelectSingleNode("//a[@class='trailer_headerTrailerBtn']");
                return linkNode?.GetAttributeValue("href", "");
            }
        }

        public static async Task<string> FindFilmUrl(string filmName, int year = 0)
        {
            string searchUrl = basicUrl + " " + filmName;

            using (HttpClient client = new HttpClient())
            {
                string html = await client.GetStringAsync(searchUrl);

                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(html);

                var filmLinks = document.DocumentNode.SelectNodes(searchLinks);
                var filmInfos = document.DocumentNode.SelectNodes(searchInfos);

                if (year != 0)
                {
                    for(int i = 0; i < filmLinks.Count; i++) 
                    {
                        var yearInfo = int.Parse(filmInfos[i*2].InnerText.Split(',')[0]);

                        if(yearInfo == year) 
                        {
                            return filmLinks[i].GetAttributeValue("href", "");
                        }
                    }
                }

                return filmLinks[0].GetAttributeValue("href", "");
            }
        }

        private static string ExtractRating(string description)
        {
            int ratingIndex = description.IndexOf(rateName);

            if (ratingIndex != -1)
            {
                int ratingValueIndex = description.IndexOf(" ", ratingIndex + rateName.Length);

                int ratingValueEndIndex = description.IndexOf(" ", ratingValueIndex + 1);

                return description.Substring(ratingValueIndex + 1, ratingValueEndIndex - ratingValueIndex - 2);
            }

            return null;
        }

        private static string ExtractYear(string description)
        {
            Match match = Regex.Match(description, datePattern);

            return match.ToString().Split()[2];
        }

        private static List<string> ExtractGenres(string description)
        {
            List<string> genres = new List<string>();

            int genreIndex = description.IndexOf(genreName);

            if (genreIndex != -1)
            {
                int genreListIndex = description.IndexOf(" ", genreIndex + "Жанр -".Length);

                int genreListEndIndex = description.IndexOf(".", genreListIndex);

                string genreList = description.Substring(genreListIndex + 1, genreListEndIndex - genreListIndex - 1);

                foreach (string genre in genreList.Split(","))
                {
                    genres.Add(genre.Trim());
                }
            }

            return genres;
        }

        private static string ExtractFilmInfo(HtmlDocument document)
        {
            HtmlNode paragraphNode = document.DocumentNode.SelectSingleNode(searchDescription);

            string text = paragraphNode.InnerText;

            text = text.Replace("\n", "").Replace("\t", "");

            string new_text= "";
            int start_index = -1;
            int end_index = -1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text.ElementAt(i) == '&')
                {
                    start_index = i;
                }
                if(text.ElementAt(i) == ';')
                {
                    end_index = i;
                }
                if(start_index != -1 && end_index == -1)
                {
                    continue;
                }
                if (start_index != -1 && end_index != -1)
                {
                    start_index = -1;
                    end_index = -1;
                    continue;
                }
                new_text = new_text.Insert(new_text.Length, text.ElementAt(i).ToString());
            }

            return new_text;
        }
    }
}
