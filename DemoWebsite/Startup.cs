using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(DemoWebsite.Startup))]
namespace DemoWebsite
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {

        }
    }
}
