namespace Movies
{
    /// <summary>
    /// How I would define the Relationships in the code. The comments show in intellisense giving feedback as
    /// someone is typing in code. Especially important when they are new to the project.
    /// </summary>
    public static class Relationships
    {
        /// <summary>
        ///     Usage <c>(<see cref="Person">Person</see>)-[:ACTED_IN]->(<see cref="Movie">Movie</see>)</c>
        /// </summary>
        public const string ActedIn = "ACTED_IN";

        /// <summary>
        ///     Usage <c>(<see cref="Person">Person</see>)-[:DIRECTED]->(<see cref="Movie">Movie</see>)</c>
        /// </summary>
        public const string Directed = "DIRECTED";

        /// <summary>
        ///     Usage <c>(<see cref="Person">Person</see>)-[:WROTE]->(<see cref="Movie">Movie</see>)</c>
        /// </summary>
        public const string Wrote = "WROTE";

        /// <summary>
        ///     Usage <c>(<see cref="Person">Person</see>)-[:FOLLOWS]->(<see cref="Person">Person</see>))</c>
        /// </summary>
        public const string Follows = "FOLLOWS";

        /// <summary>
        ///     Usage <c>(<see cref="Person">Person</see>)-[:REVIEWED]->(<see cref="Movie">Movie</see>)</c>
        /// </summary>
        public const string Reviewed = "REVIEWED";

        /// <summary>
        ///     Usage <c>(<see cref="Person">Person</see>)-[:PRODUCED]->(<see cref="Movie">Movie</see>)</c>
        /// </summary>
        public const string Produced = "PRODUCED";
    }
}