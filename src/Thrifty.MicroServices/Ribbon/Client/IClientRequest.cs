using System;

namespace Thrifty.MicroServices.Ribbon.Client
{
    public interface IClientRequest
    {
        Uri Uri { get; }
        bool Retriable { get; }
    }
}
