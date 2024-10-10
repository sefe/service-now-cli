namespace ServiceNowCLI.Core.ServiceNow
{
    public class ResultArray<T>
    {
        public T[] result;
    }

    public class ResultObject<T>
    {
        public T result;
    }

    public enum CrStates
    {
        New = -5,
        Assess = -4,
        Authorize = -3,
        Scheduled = -2,
        Implement = -1,
        Review = 0,
        Closed = 3,
        Cancelled = 4,
    }

    public enum CrTypes
    {
        Standard,
        Normal,
        Emergency
    }

    public enum CrCloseCodes
    {
        successful,
        unsuccessful,
        successful_issues
    }

    public static class CloseFields
    {
        public static readonly string Reason = "reason";
        public static readonly string CloseNotes = "close_notes";
    }
}
