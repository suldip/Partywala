namespace PartyClap.Models
{
    public class State
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public bool IsEnabled { get; set; }

        public int CityCount { get; set; }
        public int PinCodeCount { get; set; }
    }
}
