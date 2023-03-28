namespace MoviesMvcCore.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Movies;
    using Neo4j.Driver;

    [ApiController]
    [Route("api/driver/movie")]
    public class MovieDriverController : Controller
    {
        private readonly IDriver _driver;
        private readonly ILogger<MovieDriverController> _logger;

        public MovieDriverController(ILogger<MovieDriverController> logger, IDriver driver)
        {
            _logger = logger;
            _driver = driver;
        }

        /// <summary>
        ///     GET: api/driver/movie/list
        /// </summary>
        /// <returns>A list of all the <see cref="Movie"/>s in the database.</returns>
        [HttpGet("list")]
        public async Task<ActionResult<IEnumerable<Movie>>> List()
        {
            // Note the use of 'Movie.Labels' here
            var query = @$"MATCH (m:{Movie.Labels}) 
                           RETURN m";

            // We use a 'Session' to perform our queries
            var session = _driver.AsyncSession();
            // We're using the Transaction Function (https://neo4j.com/docs/driver-manual/current/session-api/#driver-simple-transaction-fn) this works with
            // retries against a cluster, but also works with a single instance, so makes our code future proof.
            var results = await session.ReadTransactionAsync(async tx =>
            {
                // This is where we actually execute our code
                var cursor = await tx.RunAsync(query);
                // Now we 'fetch' our data - if we don't have any, 'fetched' will be false.
                var fetched = await cursor.FetchAsync();
                // This is where we'll store our output.
                var output = new List<Movie>();

                // While we _have_ data...
                while (fetched)
                {
                    // Get the 'Node'
                    var node = cursor.Current["m"].As<INode>();

                    // Convert it into a Movie - this has been extracted to a method - as that's what we'd do with proper code - DRY principles!
                    var movie = ConvertToMovie(node);

                    // Add to our output
                    output.Add(movie);

                    // Do we have more?
                    fetched = await cursor.FetchAsync();
                }

                // Return our output from the Transaction Function
                return output;
            });

            // Return the Transaction Function results to the caller.
            return results;
        }

        /// <summary>
        /// This takes an <see cref="INode"/> instance and converts it to a <see cref="Movie"/> instance.
        /// </summary>
        /// <remarks>From a development point of view, this saves us writing it 10 times for 10 methods - but, it might be a bit clunky to do this
        /// for every object we want to convert to. I would advise you to look at <see cref="MovieExtensionController"/> or <see cref="MovieNeo4jClientController"/>
        /// for alternatives.</remarks>
        /// <param name="node">The <see cref="INode"/> to convert.</param>
        /// <returns>A filled <see cref="Movie"/> object.</returns>
        private static Movie ConvertToMovie(INode node)
        {
            return new Movie
            {
                Title = node.Properties["title"].As<string>(),
                //We do this as there are some Movies that don't have taglines (and indeed released years)
                Tagline = node.Properties.ContainsKey("tagline") ? node.Properties["tagline"].As<string>() : null,
                Released = node.Properties.ContainsKey("released") ? node.Properties["released"]?.As<int?>() : null
            };
        }

        /// <summary>
        /// GET: api/driver/movie/title/{movie title}
        /// </summary>
        /// <param name="title">The name of the <see cref="Movie"/> to find.</param>
        /// <returns>A <see cref="Movie"/> or <c>null</c> if the movie doesn't exist.</returns>
        [HttpGet("title/{title}")]
        public async Task<ActionResult<Movie>> GetByTitle(string title)
        {
            var query = @$"MATCH (m:{Movie.Labels}) 
                           WHERE m.title = $title 
                           RETURN m";

            var session = _driver.AsyncSession();
            var results = await session.ReadTransactionAsync(async tx =>
            {
                var cursor = await tx.RunAsync(query, new {title});
                var fetched = await cursor.FetchAsync();

                while (fetched)
                {
                    var node = cursor.Current["m"].As<INode>();
                    var movie = ConvertToMovie(node);
                    return movie;
                }

                return null;
            });

            return results;
        }

        /// <summary>
        /// GET: api/driver/movie/actors/{title}
        /// </summary>
        /// <param name="title">The name of the <see cref="Movie"/> to find.</param>
        /// <returns>The list of the people who <see cref="Relationships.ActedIn"/> the movie with the <paramref name="title"/> - or <c>null</c>.</returns>
        [HttpGet("actors/{title}")]
        public async Task<ActionResult<IEnumerable<string>>> GetActorNamesByTitle(string title)
        {
            // Note in the query we return the 'COLLECT(p.name)' as 'Names' - which is what we access later on.
            // - We could have just accessed cursor.Current["COLLECT(p.name)"] - but this is clearer.
            var query = @$"MATCH (m:{Movie.Labels})<-[:{Relationships.ActedIn}]-(p:{Person.Labels}) 
                           WHERE m.title = $title 
                           RETURN COLLECT(p.name) AS Names";

            var session = _driver.AsyncSession();
            var results = await session.ReadTransactionAsync(async tx =>
            {
                var cursor = await tx.RunAsync(query, new {title});
                var fetched = await cursor.FetchAsync();

                while (fetched)
                {
                    // As 'Names' is a list of 'string' we can just convert to an `IEnumerable<string>`
                    var names = cursor.Current["Names"].As<IEnumerable<string>>();
                    return names;
                }

                return null;
            });

            return results.ToList();
        }


        /// <summary>
        /// GET: api/driver/movie/actors
        /// </summary>
        /// <returns>The list of the people who <see cref="Relationships.ActedIn"/> a movie with the Movie title.</returns>
        [HttpGet("actors")]
        public async Task<ActionResult<IEnumerable<MovieTitleAndActors>>> GetActorNamesByMovie()
        {
            var query = @$"MATCH (m:{Movie.Labels})<-[:{Relationships.ActedIn}]-(p:{Person.Labels}) 
                           RETURN m.title AS title, COLLECT(p.name) as actors";

            var session = _driver.AsyncSession();
            var results = await session.ReadTransactionAsync(async tx =>
            {
                var cursor = await tx.RunAsync(query);
                var fetched = await cursor.FetchAsync();

                var output = new List<MovieTitleAndActors>();
                while (fetched)
                {
                    var maa = new MovieTitleAndActors
                    {
                        Title = cursor.Current["title"].As<string>(),
                        Actors = cursor.Current["actors"].As<IEnumerable<string>>()
                    };
                    output.Add(maa);
                    
                    fetched = await cursor.FetchAsync();
                }

                return output;
            });


            return results.ToList();
        }
    }
}