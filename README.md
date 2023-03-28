# Movies Mvc Core

An example ASP NET MVC Core project to access Neo4j using the official and community drivers.

This is a work in progress and will be updated when it can be.

# How do you use this?

You need a few things to get the example running:

1. **Neo4j Server 5.x** - easiest way is with [Neo4j Desktop](https://neo4j.com/download/), just create a new `5.x` version (any will work)
1. **Movies DataSet** - Once you have (1) you need to get the Movies dataset, to do that:
   1. Start Neo4j
   1. Open the browser [here](http://localhost:7474)
   1. Run `:play movies`
   1. Skip the first page, and *play* the script on page 2. 

Now you're ready!

# Controllers

There are 3 controllers to show the different approaches to coding .NET against a Neo4j Database.

## MovieDriverController (`api/driver/movie`)

This is the controller using a pure implementation of the [Neo4j.Driver](https://nuget.org/packages/Neo4j.Driver) with no extensions.


## MovieExtensionController (`api/driverextensions/movie`)

The controller using [Neo4j.Driver.Extensions](https://nuget.org/packages/Neo4j.Driver.Extensions) to reduce boiler plate code.



## MoviesNeo4jClientController (`api/neo4jclient/movie`)

The controller using [Neo4jClient](https://nuget.org/packages/Neo4jClient) - the community driver.


# Example Calls

Just some example calls for you to use against the API.

## List (`list`)

This gets all the Movies in the database

* `/<controller-specific-api>/list`


## Title (`title/<title>`)

This gets a specific Movie, provided by the `<title>` parameter:

* `/<controller-specific-api>/title/The%20Matrix`

## Actors (`actors`)

This gets all the actors by Movie (i.e. Title + List of Actors)

* `/<controller-specific-api>/actors`

## Actors (`actors/<title>`)

This gets all the actors by for a specific Movie

* `/<controller-specific-api>/actors/The%20Matrix`