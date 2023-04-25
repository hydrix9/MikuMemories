using Jering.Javascript.NodeJS;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;


namespace MikuMemories {
    public static class NodeServiceHandler
    {
        public static INodeJSService nodeJSService;

        static NodeServiceHandler()
        {
            var services = new ServiceCollection();
            services.AddNodeJS();
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            nodeJSService = serviceProvider.GetRequiredService<INodeJSService>();
        }

    }
}