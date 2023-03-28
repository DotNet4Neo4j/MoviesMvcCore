namespace Movies
{
    using System.Collections.Generic;
    using Neo4j.Driver.Extensions;
    using Newtonsoft.Json;

    public class Movie
    {
        [JsonProperty("title")] //This is for Neo4jClient
        [Neo4jProperty(Name = "title")] //This is for Neo4jDriver.Extensions
        public string Title { get; set; }

        [JsonProperty("released")]
        [Neo4jProperty(Name = "released")]
        public int? Released { get; set; }

        [JsonProperty("tagline")]
        [Neo4jProperty(Name = "tagline")]
        public string Tagline { get; set; }

        //This property is used by all our controllers, to allow us to use string interpolation to try to avoid typos
        public static string Labels => nameof(Movie);
    }

    public class MovieTitleAndActors
    {
        [JsonProperty("title")]
        [Neo4jProperty(Name = "title")]
        public string Title { get; set; }

        [JsonProperty("actors")]
        [Neo4jProperty(Name = "actors")]
        public IEnumerable<string> Actors { get; set; }

        public override string ToString()
        {
            return $"{Title} - {string.Join(",", Actors)}";
        }
    }
}