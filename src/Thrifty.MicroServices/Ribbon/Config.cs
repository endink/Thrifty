namespace Thrifty.MicroServices.Ribbon
{
    public class ClientOptions
    {
        public string ClientName { get; set; }

    }


    public class DefaultRetryPolicyOptions
    {
        public bool Enabled { get; set; }
        public bool MaxRetriesOnSameServer { get; set; }
        public bool MaxRetriesOnNextServer { get; set; }
    }
}
