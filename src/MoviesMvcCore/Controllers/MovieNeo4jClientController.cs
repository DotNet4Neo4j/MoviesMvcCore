namespace MoviesMvcCore.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Movies;
    using Neo4jClient;
    
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
        public async Task<ActionResult<IEnumerable<MovieTitleAndActors>>> GetActorNamesByMovie()
        {
            var results = await _client.Cypher.Match($"(m:{Movie.Labels})<-[:{Relationships.ActedIn}]-(p:{Person.Labels})")
                .With("m.title AS title, COLLECT(p.name) AS actors")
                .Return((title, actors) => new MovieTitleAndActors  //Here we're returning the items as a class, casting properties individually
                {
                    Title = title.As<string>(),
                    Actors = actors.As<IEnumerable<string>>()
                })
                .ResultsAsync;

            return results.ToList();
        }
    }
}