namespace XmlRpcAspNet5
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using Microsoft.AspNet.Http.Core;
    using Microsoft.AspNet.Mvc;
    using Microsoft.AspNet.Mvc.ActionConstraints;
    using Microsoft.AspNet.Mvc.Core;
    using Microsoft.AspNet.Mvc.Routing;
    using Microsoft.AspNet.Routing;
    using Microsoft.Framework.DependencyInjection;
    using Microsoft.Framework.Logging;

    public class XmlRpcActionSelector : IActionSelector
    {
        private readonly IActionSelectorDecisionTreeProvider _decisionTreeProvider;
        private readonly IActionSelector _inner;
        public XmlRpcActionSelector( IActionSelectorDecisionTreeProvider decisionTreeProvider, IServiceProvider services )
        {
            _decisionTreeProvider = decisionTreeProvider;
            _inner = new DefaultActionSelector(
                services.GetRequiredService<IActionDescriptorsCollectionProvider>(),
                decisionTreeProvider,
                services.GetRequiredServices<IActionConstraintProvider>(),
                services.GetRequiredService<ILoggerFactory>() );
        }
        public async Task<ActionDescriptor> SelectAsync( RouteContext context )
        {
            if ( context.HttpContext.Request.ContentLength == 0 )
                return await _inner.SelectAsync( context );

            var contentType = context.HttpContext.Request.ContentType;

            if ( !string.IsNullOrWhiteSpace( contentType ) &&
                !( contentType.ToLower().StartsWith( "application/xml" ) ||
                    contentType.ToLower().StartsWith( "text/xml" ) ) )
            {
                return await _inner.SelectAsync( context );
            }

            var request = context.HttpContext.Request;
            string str;
            request.EnableRewind();
            using ( var sr = new StreamReader( request.Body, Encoding.UTF8, false, 2048, true ) )
            {
                str = await sr.ReadToEndAsync();
            }
            request.Body.Position = 0;
            XDocument doc;
            try
            {
                doc = XDocument.Parse( str );
            }
            catch ( Exception )
            {
                return await _inner.SelectAsync( context );
            }

            var methodNameElement = doc.XPathSelectElement( "//methodName" );
            if ( methodNameElement == null || string.IsNullOrWhiteSpace( methodNameElement.Value ) )
                return await _inner.SelectAsync( context );

            var methodName = methodNameElement.Value;
            var tree = _decisionTreeProvider.DecisionTree;
            var matchingRouteConstraints = tree.Select( context.RouteData.Values ).ToList();

            var methodNameParts = methodName.Split( ".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries );
            if ( methodNameParts.Length != 2 )
                return await _inner.SelectAsync( context );

            var matches =
                matchingRouteConstraints.Where(
                    c => c.Name.EndsWith( methodNameParts[1], StringComparison.OrdinalIgnoreCase ) ).ToList();
            if ( matches.Count() > 1 || !matches.Any() )
                return await _inner.SelectAsync( context );

            var match = matches.Single();
            context.RouteData.Values.Add( "XmlRpcParams", match.Parameters );
            return await Task.FromResult( match );
        }
        public bool HasValidAction( VirtualPathContext context )
        {
            return _inner.HasValidAction( context );
        }
    }
}