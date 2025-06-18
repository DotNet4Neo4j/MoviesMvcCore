namespace MoviesMvcCore.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using EnsureThat;
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
        public async Task<ActionResult<IEnumerable<MovieTitleAndRelatedPeople>>> GetActorNamesByMovie()
        {
            return (await GetPeopleNamesByRelationshipTypeAndMovie(Relationships.ActedIn)).ToList();
        }

        public async Task<IEnumerable<MovieTitleAndRelatedPeople>> GetPeopleNamesByRelationshipTypeAndMovie(string relationship)
        {
            // Note - we did have to change the query slightly here - we return an anonymous type (https://neo4j.com/docs/cypher-manual/current/syntax/patterns/#cypher-pattern-properties)
            var query = @$"MATCH (m:{Movie.Labels})<-[r:{relationship}]-(p:{Person.Labels}) 
                           RETURN {{title: m.title, relationshipType: type(r), people:COLLECT(p.name)}} AS maa";

            var session = _driver.AsyncSession();
            var moviesAndActors = await session.RunReadTransactionForObjects<MovieTitleAndRelatedPeople>(query, null, "maa");
            return moviesAndActors.ToList();
        }
        
        /// <summary>
        ///     GET: api/driverextensions/movie/addPerson/name/born
        /// </summary>
        /// <param name="name">The name of the <see cref="Person"/> to add.</param>
        /// <param name="born">The birth year of the <see cref="Person"/> to add.</param>
        /// <returns>The <see cref="Person"/> that was added.</returns>
        [HttpGet("addPerson/{name}/{born}")]
        public async Task<Person> AddPerson(string name, int? born)
        {
            Ensure.That(name).IsNotEmptyOrWhiteSpace();
            var person = new Person { Name = name, Born = born };

            var query = Neo4jDriverExtraExtensions.MergePerson(null, person);

            var session = _driver.AsyncSession();
            await session.ExecuteWriteAsync(work => work.RunAsync(query));

            return person;
        }

        /// <summary>
        ///     GET: api/driverextensions/movie/addPersonToMovie/MOVIE-TITLE/RELATIONSHIP/NAME/BORN
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

            //NB - there is no difference between Neo4j.Driver and Neo4j.Driver.Extensions as the extensions are all based around READ, not WRITE methods.
            var person = new Person { Name = name };

            var query = new Query(@$"MATCH (m:{Movie.Labels})
                                          WHERE m.title = $movieTitle", new {movieTitle})
                .MergePerson(person)
                .AddToQuery($"MERGE (m)<-[:{relationship}]-(p)");

            var session = _driver.AsyncSession();
            await session.ExecuteWriteAsync(work => work.RunAsync(query));
            
            return await GetPeopleNamesByRelationshipTypeAndMovie(relationship);
        }
    }
}