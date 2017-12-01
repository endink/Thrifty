namespace Thrifty.Samples.Common
{
    [ThriftStruct("CallResult")]
    public class CallResult<T>
    {
        [ThriftField(1)]
        public string ErrorMessage { get; set; }
        [ThriftField(2)]
        public T Payload { get; set; }
    }
}
