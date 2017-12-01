namespace Ribbon.Test.Just4Fun
{
    /// <summary>
    /// 远程过程参数
    /// </summary>
    public interface IProcedureParameter
    {
        /// <summary>
        /// 参数名
        /// </summary>
        string Name { get; }
        /// <summary>
        /// 参数值
        /// </summary>
        object Value { get; }
    }
}
