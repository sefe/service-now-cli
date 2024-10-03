namespace ServiceNowCLI.Config.Dtos
{
    internal class DorcPropertyValue
    {
        public int Id { get; set; }

        public string Value { get; set; }

        public DorcProperty Property { get; set; }

        public string PropertyValueFilter { get; set; }

        public int? PropertyValueFilterId { get; set; }

        public int Priority { get; set; }

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, {nameof(Value)}: {Value}, {nameof(Property)}: {Property}, {nameof(PropertyValueFilter)}: {PropertyValueFilter}, {nameof(PropertyValueFilterId)}: {PropertyValueFilterId}, {nameof(Priority)}: {Priority}";
        }
    }
}
