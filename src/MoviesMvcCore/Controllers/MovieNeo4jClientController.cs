namespace MoviesMvcCore.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using EnsureThat;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Movies;
    using Neo4jClient;
    using Neo4jClient.Cypher;

    [ApiController]
    [Route("api/neo4jclient/movie")]
    public class MovieNeo4jClientController : Controller
    {
        private readonly IGraphClient _client;
        private readonly ILogger<MovieNeo4jClientController> _logger;

        public MovieNeo4jClientController(ILogger<MovieNeo4jClientController> logger, IGraphClient client)
        {
            _logger = logger;
            _client = client;
        }

        /// <summary>
        ///     GET: api/neo4jclient/movie/list
        /// </summary>
        /// <returns>A list of all the <see cref="Movie" />s in the database.</returns>
        [HttpGet("list")]
        public async Task<ActionResult<IEnumerable<Movie>>> List()
        {
            var movies = await _client.Cypher.Match($"(m:{Movie.Labels})") 
                .Return(m => m.As<Movie>()) //Note - this will convert the 'm' result to our Movie
                .ResultsAsync;
            
            return movies.ToList();
        }

        /// <summary>
        ///     GET: api/neo4jclient/movie/title/{movie title}
        /// </summary>
        /// <param name="title">The name of the <see cref="Movie" /> to find.</param>
        /// <returns>A <see cref="Movie" /> or <c>null</c> if the movie doesn't exist.</returns>
        [HttpGet("title/{title}")]
        public async Task<ActionResult<Movie>> GetByTitle(string title)
        {
            var movies = await _client.Cypher.Match($"(m:{Movie.Labels})")
                .Where((Movie m) => m.Title == title) //This auto generates a 'p0' parameter for our query.
                .Return(m => m.As<Movie>())
                .ResultsAsync;

            return movies.SingleOrDefault();
        }

        /// <summary>
        ///     GET: api/neo4jclient/movie/actors/{title}
        /// </summary>
        /// <param name="title">The name of the <see cref="Movie" /> to find.</param>
        /// <returns>
        ///     The list of the people who <see cref="Relationships.ActedIn" /> the movie with the <paramref name="title" /> -
        ///     or <c>null</c>.
        /// </returns>
        [HttpGet("actors/{title}")]
        public async Task<ActionResult<IEnumerable<string>>> GetActorNamesByTitle(string title)
        {
            var actors = await _client.Cypher.Match($"(m:{Movie.Labels})<-[:{Relationships.ActedIn}]-(p:{Person.Labels})")
                .Where((Movie m) => m.Title == title)
                .Return(p => p.As<Person>().Name) //Here we're picking just a specific property from the object
                .ResultsAsync;

            return actors.ToList();
        }


        /// <summary>
        ///     GET: api/neo4jclient/movie/actors
        /// </summary>
        /// <returns>The list of the people who <see cref="Relationships.ActedIn" /> a movie with the Movie title.</returns>
        [HttpGet("actors")]
        public async Task<ActionResult<IEnumerable<MovieTitleAndRelatedPeople>>> GetActorNamesByMovie()
        {
            return (await GetPeopleNamesByRelationshipTypeAndMovie(Relationships.ActedIn)).ToList();
        }

        /// <summary>
        ///     Gets a list of the <see cref="Person"/>s with the <paramref name="relationshipType"/> relationship with any <see cref="Movie"/>.
        /// </summary>
        /// <param name="relationshipType">The type of <see cref="Relationships"/> to find. NB This is NOT checked for validity.</param>
        /// <returns>The list of the people who have the <paramref name="relationshipType"/> with any <see cref="Movie"/>.</returns>
        private async Task<IEnumerable<MovieTitleAndRelatedPeople>> GetPeopleNamesByRelationshipTypeAndMovie(string relationshipType)
        {
            var results = await _client.Cypher.Match($"(m:{Movie.Labels})<-[r:{relationshipType}]-(p:{Person.Labels})")
                .With("m.title AS title, type(r) AS rel, COLLECT(p.name) AS people")
                .Return((title, rel, people) => new MovieTitleAndRelatedPeople  //Here we're returning the items as a class, casting properties individually
                {
                    Title = title.As<string>(),
                    RelationshipType = rel.As<string>(),
                    People = people.As<IEnumerable<string>>()
                })
                .ResultsAsync;

            return results;
        }

        /// <summary>
        ///     GET: api/neo4jclient/movie/addPerson/name/born
        /// </summary>
        /// <param name="name">The name of the <see cref="Person"/> to add.</param>
        /// <param name="born">The birth year of the <see cref="Person"/> to add.</param>
        /// <returns>The <see cref="Person"/> that was added.</returns>
        [HttpGet("addPerson/{name}/{born}")]
        public async Task<Person> AddPerson(string name, int? born)
        {
            Ensure.That(name).IsNotEmptyOrWhiteSpace();
            var person = new Person { Name = name, Born = born };

            await _client.Cypher.Write.MergePerson(person).ExecuteWithoutResultsAsync();
            return person;
        }
        
        /// <summary>
        ///     GET: api/neo4jclient/movie/addPersonToMovie/MOVIE-TITLE/RELATIONSHIP/NAME/BORN
        /// </summary>
        /// <remarks>
        /// Using GET here really to simplify how you can call this from a browser - this *should* be a POST, and we would
        /// be using a <see cref="Person"/> object as a parameter instead of just strings.
        /// </remarks>
        /// <param name="movieTitle">The title of the <see cref="Movie"/> to add the <see cref="Person"/> to.</param>
        /// <param name="relationship">The relationship type to create.</param>
        /// <param name="name">The name of the <see cref="Person"/> to add.</param>
        /// <returns>The list of the people who have the <paramref name="relationship"/> with any <see cref="Movie"/>.</returns>
        [HttpGet("addPersonToMovie/{movieTitle}/{relationship}/{name}")]
        public async Task<IEnumerable<MovieTitleAndRelatedPeople>> AddPerson(string movieTitle, string relationship, string name)
        {
            Ensure.That(movieTitle).IsNotEmptyOrWhiteSpace();
            Ensure.That(relationship).IsNotEmptyOrWhiteSpace();
            Ensure.That(name).IsNotEmptyOrWhiteSpace();

            var person = new Person { Name = name };
            var query = _client.Cypher.Match($"(m:{Movie.Labels})")
                .Where((Movie m) => m.Title == movieTitle)
                .MergePerson(person)
                .Merge($"(m)<-[:{relationship}]-(p)");

            await query.Write.ExecuteWithoutResultsAsync();
            return await GetPeopleNamesByRelationshipTypeAndMovie(relationship);
        }
    }

    public static class Neo4jClientQueryExtensions
    {
        /// <summary>
        /// MERGEs a <see cref="Person"/> into the database.
        /// </summary>
        /// <remarks>
        ///     This uses a parameter called 'person' which could conflict if already (or subsequently) used in the <paramref name="query"/>.
        ///     To resolve this, you can pass in a different parameter name as an argument.
        ///     This is primarily to show 'reuse' of Cypher within a query.
        /// </remarks>
        /// <param name="query">The <see cref="ICypherFluentQuery"/> to add to.</param>
        /// <param name="person">The <see cref="Person"/> to MERGE into the database.</param>
        /// <param name="parameterName">DEFAULT: 'person' - use this to change the parameter used, in case you have used 'person' in another area of the <paramref name="query"/>, or intend to.</param>
        /// <returns>An <see cref="ICypherFluentQuery"/> instance to continue querying with.</returns>
        public static ICypherFluentQuery MergePerson(this ICypherFluentQuery query, Person person, string parameterName = "person")
        {
            return query
                .Merge($"(p:{Person.Labels} {{name: {parameterName}.name}}")
                .OnCreate().Set($"p = ${parameterName}")
                .WithParam(parameterName, person);
        }
    }
}