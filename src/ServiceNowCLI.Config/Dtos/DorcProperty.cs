namespace ServiceNowCLI.Config.Dtos
{
    internal class DorcProperty
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public bool Secure { get; set; }

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, {nameof(Name)}: {Name}, {nameof(Secure)}: {Secure}";
        }
    }
}
