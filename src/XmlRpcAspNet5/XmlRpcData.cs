namespace XmlRpcAspNet5
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Linq;
    using System.Xml.Linq;

    public static class XmlRpcData
    {
        //iso8601 often come in slightly different flavours rather than the standard "s" that string.format supports.
        //http://stackoverflow.com/a/17752389
        private static readonly string[] Formats = {
            // Basic formats
            "yyyyMMddTHHmmsszzz",
            "yyyyMMddTHHmmsszz",
            "yyyyMMddTHHmmssZ",
            // Extended formats
            "yyyy-MM-ddTHH:mm:sszzz",
            "yyyy-MM-ddTHH:mm:sszz",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyyMMddTHH:mm:ss:zzz",
            "yyyyMMddTHH:mm:ss:zz",
            "yyyyMMddTHH:mm:ss:Z",
            "yyyyMMddTHH:mm:ss",
            // All of the above with reduced accuracy
            "yyyyMMddTHHmmzzz",
            "yyyyMMddTHHmmzz",
            "yyyyMMddTHHmmZ",
            "yyyy-MM-ddTHH:mmzzz",
            "yyyy-MM-ddTHH:mmzz",
            "yyyy-MM-ddTHH:mmZ",
            // Accuracy reduced to hours
            "yyyyMMddTHHzzz",
            "yyyyMMddTHHzz",
            "yyyyMMddTHHZ",
            "yyyy-MM-ddTHHzzz",
            "yyyy-MM-ddTHHzz",
            "yyyy-MM-ddTHHZ"
        };
        public static object DeserialiseValue( XElement value, Type targetType )
        {
            //First check that we have a 'value' node that's been passed in
            if ( !string.Equals( value.Name.LocalName, "value", StringComparison.OrdinalIgnoreCase ) )
                throw new ArgumentException( "The supplied node is not a 'value' node." );

            var dataTypeNode = value.Elements().Single();
            var dataType = dataTypeNode.Name.LocalName;

            if ( dataType == "string" )
            {
                return value.Value;
            }
            if ( dataType == "int" || dataType == "i4" )
            {
                return int.Parse( value.Value );
            }
            if ( dataType == "i8" )
            {
                return long.Parse( value.Value );
            }
            if ( dataType == "double" )
            {
                return double.Parse( value.Value );
            }
            if ( dataType == "boolean" )
            {
                return value.Value == "1";
            }
            if ( dataType == "dateTime.iso8601" )
            {
                return DateTime.ParseExact( value.Value, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None );
            }
            if ( dataType == "array" )
            {
                var entries = value.Element( "array" ).Element( "data" ).Elements( "value" ).ToList();
                var targetArray = Array.CreateInstance( targetType.GetElementType(), entries.Count() );
                var index = 0;
                foreach ( var entry in entries )
                {
                    var propertyValue = DeserialiseValue( entry, targetType.GetElementType() );
                    targetArray.SetValue( propertyValue, index );
                    index++;
                }
                return targetArray;
            }
            if ( dataType == "struct" )
            {
                var members = value.Element( "struct" ).Elements( "member" );
                var targetObject = Activator.CreateInstance( targetType );
                var propertyInfos = targetType.GetFields();
                foreach ( var member in members )
                {
                    var propertyInfo = propertyInfos.SingleOrDefault( x => x.Name == member.Element( "name" ).Value );
                    if ( propertyInfo != null )
                    {
                        var propertyValue = DeserialiseValue( member.Element( "value" ), propertyInfo.FieldType );
                        propertyInfo.SetValue( targetObject, propertyValue );
                    }
                }
                return targetObject;
            }
            throw new ArgumentException( $"The supplied XML-RPC value '{dataType}' is not recognised" );
        }
        public static XElement SerialiseValue( object value )
        {
            var root = new XElement( "value" );

            if ( value == null )
            {
                root.Add( new XElement( "nil" ) );
            }
            else if ( IsPrimitiveXmlRpcType( value.GetType() ) )
            {
                root.Add( SerialisePrimitive( value ) );
            }
            else if ( value.GetType() is IEnumerable || value.GetType().IsArray )
            {
                root.Add( SerialiseEnumerable( value as IEnumerable ) );
            }
            else
            {
                root.Add( SerialiseStrut( value ) );
            }

            return root;
        }
        private static bool IsPrimitiveXmlRpcType( Type type )
        {
            return type.IsPrimitive || type.Equals( typeof( string ) ) || type.Equals( typeof( DateTime ) ) ||
                type.Equals( typeof( long ) );
        }
        private static XElement SerialiseStrut( object value )
        {
            var root = new XElement( "struct" );

            var propInfos = value.GetType().GetFields();
            foreach ( var propInfo in propInfos )
            {
                var member = new XElement( "member" );

                member.Add(
                    new XElement( "name", propInfo.Name ),
                    SerialiseValue( propInfo.GetValue( value ) ) );

                root.Add( member );
            }

            return root;
        }
        private static XElement SerialiseEnumerable( IEnumerable values )
        {
            var enumerableElement = new XElement( "array" );
            var dataElement = new XElement( "data" );

            foreach ( var value in values )
            {
                dataElement.Add( SerialiseValue( value ) );
            }

            enumerableElement.Add( dataElement );

            return enumerableElement;
        }
        private static XElement SerialisePrimitive( object value )
        {
            if ( value is string )
            {
                return new XElement( "string", value );
            }
            if ( value is int )
            {
                return new XElement( "int", value.ToString() );
            }
            if ( value is long )
            {
                return new XElement( "i8", value.ToString() );
            }
            if ( value is double )
            {
                return new XElement( "double", value.ToString() );
            }
            if ( value is bool )
            {
                return new XElement( "boolean", (bool)value ? "1" : "0" );
            }
            if ( value is DateTime )
            {
                return new XElement( "dateTime.iso8601", ( (DateTime)value ).ToString( "s" ) );
            }

            throw new ArgumentException( $"We cannot encode the primitive '{value.GetType()}'" );
        }
    }
}