namespace XmlRpcAspNet5
{
    using Microsoft.AspNet.Mvc;
    using Microsoft.Framework.DependencyInjection;

    public static class XmlRpcServicesExtension
    {
        public static IServiceCollection AddXmlRpc( this IServiceCollection services )
        {
            services.Configure<MvcOptions>(
                options => { options.ValueProviderFactories.Add( new XmlRpcValuesProviderFactory() ); } );

            services.AddTransient<IActionSelector, XmlRpcActionSelector>();

            return services;
        }
    }
}