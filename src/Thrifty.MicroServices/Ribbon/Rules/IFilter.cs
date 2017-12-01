using System.Collections.Generic;

namespace Thrifty.MicroServices.Ribbon.Rules
{
    public interface IFilter
    {
        IEnumerable<Server> Filtration(IEnumerable<Server> servers);
    }
}
