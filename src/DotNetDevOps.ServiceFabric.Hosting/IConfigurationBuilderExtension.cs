using Microsoft.Extensions.Configuration;

namespace DotNetDevOps.ServiceFabric.Hosting
{
    public interface IConfigurationBuilderExtension
    {
        IConfigurationBuilder Extend(IConfigurationBuilder cbuilder);
    }


}
