namespace XmlRpcAspNet5
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Microsoft.AspNet.Http;
    using Microsoft.AspNet.Mvc;

    public class XmlRpcActionResult : ActionResult
    {
        private readonly XDocument _responseObject;
        public XmlRpcActionResult( object data )
        {
            _responseObject = new XDocument( new XElement( "methodResponse" ) );

            if ( data is Exception )
            {
                //Encode as a fault
                _responseObject.Element( "methodResponse" )?.Add(
                    new XElement(
                        "fault",
                        new XElement(
                            "value",
                            new XElement(
                                "string",
                                ( data as Exception ).Message ) ) ) );
            }
            else
            {
                //Encode as params
                _responseObject.Element( "methodResponse" )?.Add(
                    new XElement(
                        "params",
                        new XElement(
                            "param",
                            XmlRpcData.SerialiseValue( data ) ) ) );
            }
        }
        public override void ExecuteResult( ActionContext context )
        {
            if ( _responseObject != null )
            {
                var response = _responseObject.ToString();
                context.HttpContext.Response.ContentType = "text/xml";

                context.HttpContext.Response.Headers["content-length"] =
                    Encoding.UTF8.GetBytes( response ).Length.ToString();
                context.HttpContext.Response.WriteAsync( response ).RunSynchronously();
            }
        }
        public override Task ExecuteResultAsync( ActionContext context )
        {
            if ( _responseObject != null )
            {
                var response = _responseObject.ToString();
                context.HttpContext.Response.ContentType = "text/xml";

                context.HttpContext.Response.Headers["content-length"] =
                    Encoding.UTF8.GetBytes( response ).Length.ToString();
                return context.HttpContext.Response.WriteAsync( response );
            }

            return base.ExecuteResultAsync( context );
        }
    }
}