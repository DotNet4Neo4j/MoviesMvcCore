namespace Movies
{
    using Neo4j.Driver.Extensions;
    using Neo4j.Driver.Mapping;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public class Movie
    {
        [MappingSource("title")] //This is for the inbuilt OGM of the Official Driver
        [MappingOptional]
        [JsonProperty("title")] //This is for Neo4jClient
        [Neo4jProperty(Name = "title")] //This is for Neo4jDriver.Extensions
        public string Title { get; set; }

        [MappingSource("released")]
        [MappingOptional]
        [JsonProperty("released")]
        [Neo4jProperty(Name = "released")]
        public int? Released { get; set; }

        [MappingSource("tagline")]
        [MappingOptional]
        [JsonProperty("tagline")]
        [Neo4jProperty(Name = "tagline")]
        public string Tagline { get; set; }

        //This property is used by all our controllers, to allow us to use string interpolation to try to avoid typos
        public static string Labels => nameof(Movie);
    }

    public class MovieTitleAndRelatedPeople
    {
        [JsonProperty("title")]
        [Neo4jProperty(Name = "title")]
        [MappingSource("title")]
        public string Title { get; set; }

        [JsonProperty("relationshipType")]
        [Neo4jProperty(Name = "relationshipType")]
        [MappingSource("relationshipType")]
        public string RelationshipType { get; set; }

        /// <summary>
        /// Gets or sets the list of people associated with a <see cref="Title"/>.
        /// </summary>
        /// <remarks>This property is a <c>List{T}</c> because the object mapping in the
        /// driver needs an instance class to create.</remarks>
        [JsonProperty("people")]
        [Neo4jProperty(Name = "people")]
        [MappingSource("people")]
        public List<string> People { get; set; }

        public override string ToString()
        {
            return $"{Title} - {string.Join(",", People)}";
        }
    }
}