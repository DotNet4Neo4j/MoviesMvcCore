namespace Movies
{
    using Newtonsoft.Json;

    public class Person
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("born")]
        public int? Born { get; set; }

        public static string Labels => nameof(Person);
    }

    /// <summary>
    /// This class isn't actually used, but is here to show an example (as with <see cref="Director"/>) of how you can
    /// use the <see cref="Actor.Labels"/> property to have more than one Label for specific Node types.
    /// </summary>
    public class Actor : Person
    {
        public new static string Labels => $"{Person.Labels}:Actor";
    }

    public class Director : Person
    {
        public new static string Labels => $"{Person.Labels}:Director";
    }
}