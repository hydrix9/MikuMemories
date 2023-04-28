using System.Collections.Generic;
using System.Threading.Tasks;

namespace MikuMemories {
    public interface IAction
    {
        string Name { get; }
        Task ExecuteAsync(ConversationContext context, IDictionary<string, object> parameters);
    }

}