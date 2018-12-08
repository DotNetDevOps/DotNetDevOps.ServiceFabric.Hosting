using DotNetDevOps.ServiceFabric.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.ServiceFabric.Services.Remoting;
using System;
using Xunit;

namespace DotNetDevOps.ServiceFabric.Hosting.IntegrationTest
{
    public interface IMyService : IService
    {

    }
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var host = new FabricHostBuilder()
                .WithServiceProxy< IMyService>("aa","a");
                

        



        }
    }
}
