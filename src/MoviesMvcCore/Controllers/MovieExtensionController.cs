namespace MoviesMvcCore.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Movies;
    using Neo4j.Driver;
    using Neo4j.Driver.Extensions;

    [ApiController]
    [Route("api/driverextensions/movie")]
    public class MovieExtensionController : Controller
    {
        private readonly IDriver _driver;
        private readonly ILogger<MovieExtensionController> _logger;

        public MovieExtensionController(ILogger<MovieExtensionController> logger, IDriver driver)
        {
            _logger = logger;
            _driver = driver;
        }

        /// <summary>
        ///     GET: api/driverextensions/movie/list
        /// </summary>
        /// <returns>A list of all the <see cref="Movie" />s in the database.</returns>
        [HttpGet("list")]
        public async Task<ActionResult<IEnumerable<Movie>>> List()
        {
            // Note the use of 'Movie.Labels' here
            var query = @$"MATCH (m:{Movie.Labels}) 
                           RETURN m";

            // We use a 'Session' to perform our queries
            var session = _driver.AsyncSession();

            // We're pulling a Movie from the query
            var movies = await session.RunReadTransactionForObjects<Movie>(query, null, "m");
            return movies.ToList();
        }

        /// <summary>
        ///     GET: api/driverextensions/movie/title/{movie title}
        /// </summary>
        /// <param name="title">The name of the <see cref="Movie" /> to find.</param>
        /// <returns>A <see cref="Movie" /> or <c>null</c> if the movie doesn't exist.</returns>
        [HttpGet("title/{title}")]
        public async Task<ActionResult<Movie>> GetByTitle(string title)
        {
            var query = @$"MATCH (m:{Movie.Labels}) 
                           WHERE m.title = $title 
                           RETURN m";

            var session = _driver.AsyncSession();
            return (await session.RunReadTransactionForObjects<Movie>(query, new {title}, "m")).SingleOrDefault();
        }

        /// <summary>
        ///     GET: api/driverextensions/movie/actors/{title}
        /// </summary>
        /// <param name="title">The name of the <see cref="Movie" /> to find.</param>
        /// <returns>
        ///     The list of the people who <see cref="Relationships.ActedIn" /> the movie with the <paramref name="title" /> -
        ///     or <c>null</c>.
        /// </returns>
        [HttpGet("actors/{title}")]
        public async Task<ActionResult<IEnumerable<string>>> GetActorNamesByTitle(string title)
        {
            // Note in the query we return the 'COLLECT(p.name)' as 'Names' - which is what we access later on.
            // - We could have just accessed cursor.Current["COLLECT(p.name)"] - but this is clearer.
            var query = @$"MATCH (m:{Movie.Labels})<-[:{Relationships.ActedIn}]-(p:{Person.Labels}) 
                           WHERE m.title = $title 
                           RETURN COLLECT(p.name) AS Names";

            var session = _driver.AsyncSession();
            var results = await session.RunReadTransaction<IEnumerable<string>>(query, new {title}, "Names");

            return results.SingleOrDefault()?.ToList();
        }


        /// <summary>
        ///     GET: api/driverextensions/movie/actors
        /// </summary>
        /// <returns>The list of the people who <see cref="Relationships.ActedIn" /> a movie with the Movie title.</returns>
        [HttpGet("actors")]
        public async Task<ActionResult<IEnumerable<MovieTitleAndActors>>> GetActorNamesByMovie()
        {
            // Note - we did have to change the query slightly here - we return an anonymous type (https://neo4j.com/docs/cypher-manual/current/syntax/patterns/#cypher-pattern-properties)
            var query = @$"MATCH (m:{Movie.Labels})<-[:{Relationships.ActedIn}]-(p:{Person.Labels}) 
                           RETURN {{title: m.title, actors:COLLECT(p.name)}} AS maa";

            var session = _driver.AsyncSession();
            var moviesAndActors = await session.RunReadTransactionForObjects<MovieTitleAndActors>(query, null, "maa");
            return moviesAndActors.ToList();
        }
    }
}