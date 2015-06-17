namespace XmlRpcAspNet5
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Microsoft.AspNet.Mvc;
    using Microsoft.AspNet.Mvc.ModelBinding;
    using Microsoft.AspNet.Http.Core;
    using System.Linq;

    public class XmlRpcValueProvider : IValueProvider
    {
        private readonly ValueProviderFactoryContext _context;
        public XmlRpcValueProvider( ValueProviderFactoryContext context )
        {
            _context = context;
        }

        public Task<bool> ContainsPrefixAsync( string prefix )
        {
            throw new NotImplementedException();
        }
        public async Task<ValueProviderResult> GetValueAsync( string key )
        {
            if ( !_context.RouteValues.ContainsKey( "XmlRpcParams" ) )
                return null;

            var request = _context.HttpContext.Request;

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
                return null;
            }

            var par = doc.Descendants( "param" ).ToList();

            var xmlRpcParams = (IList<ParameterDescriptor>)_context.RouteValues["XmlRpcParams"];
            var matchedParam = xmlRpcParams.FirstOrDefault( p => p.Name == key );
            if ( matchedParam != null )
            {
                var paramIndex = xmlRpcParams.IndexOf( matchedParam );
                var p = par[paramIndex];
                var model = XmlRpcData.DeserialiseValue( p.Elements( "value" ).Single(), matchedParam.ParameterType );
                return new ValueProviderResult( model, key, CultureInfo.InvariantCulture );
            }

            return null;
        }
    }
}
