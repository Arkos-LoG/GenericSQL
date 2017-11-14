using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SuretyTrust.Common.GenericSQL
{
	// This is the one that was most useful for what I was doing   http://www.codeproject.com/Articles/742461/Csharp-Using-Reflection-and-Custom-Attributes-to-M    
	// http://stackoverflow.com/questions/4879521/creating-custom-attribute-in-c-sharp
	// http://msdn.microsoft.com/en-us/library/aa288454(v=vs.71).aspx  |  http://msdn.microsoft.com/en-us/library/z919e8tw.aspx


	[AttributeUsage(AttributeTargets.All)]
	public class GnSqlIgnoreOnInsertAttribute : Attribute { }


	[AttributeUsage(AttributeTargets.All)] // http://stackoverflow.com/questions/23281926/parse-stringvalueattribute-to-return-enum
	public class StringValueAttribute : Attribute
	{
		private string _value;
		public StringValueAttribute(string value)
		{
			_value = value;
		}

		public string Value
		{
			get { return _value; }
		}

		public static string GetStringValue(Enum value)
		{
			Type type = value.GetType();
			FieldInfo fi = type.GetRuntimeField(value.ToString());
			return (fi.GetCustomAttributes(typeof(StringValueAttribute), false).FirstOrDefault() as StringValueAttribute).Value;
		}
	}
}
