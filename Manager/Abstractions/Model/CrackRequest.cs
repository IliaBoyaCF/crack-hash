namespace Manager.Abstractions.Model
{
    public class CrackRequest
    {
        public required string Hash { get; set; }
        public required int MaxLength { get; set; }
    }
}
