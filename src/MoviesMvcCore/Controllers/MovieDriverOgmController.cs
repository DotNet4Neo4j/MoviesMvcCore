namespace MoviesMvcCore.Controllers;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Movies;
using Neo4j.Driver;
using Neo4j.Driver.Mapping;

[ApiController]
[Route("api/driverogm/movie")]
public class MovieDriverOgmController : Controller
{
    private static QueryConfig ReadConfig => new QueryConfig(RoutingControl.Readers);
    private static QueryConfig WriteConfig => new QueryConfig(RoutingControl.Writers);
    
    private readonly IDriver _driver;
    private readonly ILogger<MovieDriverOgmController> _logger;

    public MovieDriverOgmController(ILogger<MovieDriverOgmController> logger, IDriver driver)
    {
        _logger = logger;
        _driver = driver;
    }

    /// <summary>
    ///     GET: api/driver/movie/list
    /// </summary>
    /// <returns>A list of all the <see cref="Movie" />s in the database.</returns>
    [HttpGet("list")]
    public async Task<ActionResult<IEnumerable<Movie>>> List()
    {
        // Note the use of 'Movie.Labels' here
        // Also - in order to use the `[MappingSource]` attribute for an object, we need to 
        //        alias our output to match exactly what we put into the attribute.
        //        e.g. if we have:
        //            [MappingSource("title")]
        //            public string Title {get;set;}
        //
        //        we would need to ensure that the output uses
        //            ... AS title
        //
        //        to get it to map.
        var query = @$"MATCH (m:{Movie.Labels}) 
                       RETURN m.title AS title, m.tagline AS tagline, m.released AS released";

        var queryResults = await _driver.ExecutableQuery(query)
            .WithConfig(ReadConfig)
            .ExecuteAsync();

        //Select the results into an output structure
        var results = queryResults.Result.Select(result => result.AsObject<Movie>()).ToList();

        // Return the results to the caller.
        return results;
    }

    /// <summary>
    ///     GET: api/driver/movie/title/{movie title}
    /// </summary>
    /// <param name="title">The name of the <see cref="Movie" /> to find.</param>
    /// <returns>A <see cref="Movie" /> or <c>null</c> if the movie doesn't exist.</returns>
    [HttpGet("title/{title}")]
    public async Task<ActionResult<Movie>> GetByTitle(string title)
    {
         var query = @$"MATCH (m:{Movie.Labels}) 
                        WHERE m.title = $title 
                        RETURN m.title AS title, m.released AS released, m.tagline AS tagline";

         var queryResults = await _driver.ExecutableQuery(query)
             .WithConfig(ReadConfig)
             .WithParameters(new { title })
             .ExecuteAsync();

         return queryResults.Result.FirstOrDefault()?.AsObject<Movie>();
    }

    /// <summary>
    ///     GET: api/driver/movie/actors/{title}
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

        var queryResults = await _driver.ExecutableQuery(query)
            .WithParameters(new { title })
            .WithConfig(ReadConfig)
            .ExecuteAsync();

        return queryResults.Result.FirstOrDefault()?["Names"].As<IEnumerable<string>>().ToList();
    }

    /// <summary>
    ///     GET: api/driver/movie/actors
    /// </summary>
    /// <returns>The list of the people who <see cref="Relationships.ActedIn" /> a movie with the Movie title.</returns>
    [HttpGet("actors")]
    public async Task<ActionResult<IEnumerable<MovieTitleAndRelatedPeople>>> GetActorNamesByMovie()
    { 
        return (await GetPeopleNamesByRelationshipTypeAndMovie(Relationships.ActedIn)).ToList();
    }

    private async Task<IEnumerable<MovieTitleAndRelatedPeople>> GetPeopleNamesByRelationshipTypeAndMovie(string relationshipType)
    {
        var query = @$"MATCH (m:{Movie.Labels})<-[r:{relationshipType}]-(p:{Person.Labels}) 
                       RETURN m.title AS title, type(r) AS relationshipType, COLLECT(p.name) AS people";

        var queryResults = await _driver.ExecutableQuery(query)
            .WithConfig(ReadConfig)
            .ExecuteAsync();

        var results = queryResults.Result.Select(r => r.AsObject<MovieTitleAndRelatedPeople>());
        return results.ToList();
    }

    /// <summary>
    ///     GET: api/driverogm/movie/addPerson/name/born
    /// </summary>
    /// <param name="name">The name of the <see cref="Person" /> to add.</param>
    /// <param name="born">The birth year of the <see cref="Person" /> to add.</param>
    /// <returns>The <see cref="Person" /> that was added.</returns>
    [HttpGet("addPerson/{name}/{born}")]
    public async Task<Person> AddPerson(string name, int? born)
    {
        Ensure.That(name).IsNotEmptyOrWhiteSpace();
        var person = new Person { Name = name, Born = born };


        var mergeQuery = @$"MERGE (p:{Person.Labels} {{ name:$person.{nameof(Person.Name)}}})
                            ON CREATE SET p = $person";

        await _driver.ExecutableQuery(mergeQuery)
            .WithParameters(new {person})
            .WithConfig(WriteConfig)
            .ExecuteAsync();
        
        return person;
    }

    /// <summary>
    ///     GET: api/driverogm/movie/addPersonToMovie/MOVIE-TITLE/RELATIONSHIP/NAME/BORN
    /// </summary>
    /// <remarks>
    ///     Using GET here really to simplify how you can call this from a browser - this *should* be a POST, and we would
    ///     be using a <see cref="Person" /> object as a parameter instead of just strings.
    /// </remarks>
    /// <param name="movieTitle">The title of the <see cref="Movie" /> to add the <see cref="Person" /> to.</param>
    /// <param name="relationship">The relationship type to create.</param>
    /// <param name="name">The name of the <see cref="Person" /> to add.</param>
    /// <returns>The list of the people who have the <paramref name="relationship" /> with any <see cref="Movie" />.</returns>
    [HttpGet("addPersonToMovie/{movieTitle}/{relationship}/{name}")]
    public async Task<IEnumerable<MovieTitleAndRelatedPeople>> AddPerson(string movieTitle, string relationship, string name)
    {
        Ensure.That(movieTitle).IsNotEmptyOrWhiteSpace();
        Ensure.That(relationship).IsNotEmptyOrWhiteSpace();
        Ensure.That(name).IsNotEmptyOrWhiteSpace();
        
        var person = new Person { Name = name };

        var queryText =
            $@"MATCH (m:{Movie.Labels})
WHERE m.title = $movieTitle
MERGE (p:{Person.Labels} {{ name:$person.{nameof(Person.Name)}}})
ON CREATE SET p = $person
MERGE (m)<-[:{relationship}]-(p)";
        

        await _driver.ExecutableQuery(queryText)
            .WithConfig(WriteConfig)
            .WithParameters(new {person, movieTitle})
            .ExecuteAsync();

        return await GetPeopleNamesByRelationshipTypeAndMovie(relationship);
    }
}