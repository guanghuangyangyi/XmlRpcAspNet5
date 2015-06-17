namespace XmlRpcAspNet5
{
    using Microsoft.AspNet.Mvc.ModelBinding;

    public class XmlRpcValuesProviderFactory : IValueProviderFactory
    {
        public IValueProvider GetValueProvider( ValueProviderFactoryContext context )
        {
            return new XmlRpcValueProvider( context );
        }
    }
}