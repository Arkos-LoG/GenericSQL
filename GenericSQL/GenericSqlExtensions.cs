using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuretyTrust.Common.GenericSQL
{
	public static class GenericSqlExtensions
	{

		public static string ValueOf(this KeyValuePair<string, GnSqlRowItem> source, string strKey)
		{
			var list = new List<KeyValuePair<string, GnSqlRowItem>> { source };
			var dict = list.ToDictionary(x => x.Key, x => x.Value);

			var e = (dict.FirstOrDefault(k => k.Key.Equals(strKey))).Value;
			if (e != null)
				return e.Value;
			else
				return null;

		}

		public static dynamic ValueOf(this Dictionary<string, GnSqlRowItem> source, string strKey)
		{
			var e = (source.FirstOrDefault(k => k.Key.Equals(strKey))).Value;
			if (e != null)
				return e.Value;
			else
				return null;
		}

		public static GnSqlRowItem ItemOf(this Dictionary<string, GnSqlRowItem> source, string strKey)
		{
			return ((source.FirstOrDefault(k => k.Key.Equals(strKey))).Value);
		}

	}
}


//
// KEEP FOR NOW; STORAGE ON ADD METHODS TO ANONOMOUS TYPES
//
//public static class GenericSqlStatics
//{

//	// func c#: http://msdn.microsoft.com/en-us/library/bb549151(v=vs.110).aspx
//	//var x2 = new { FieldName, BusinessAction, GenericSqlStatics.Property };

//	//public static Func<dynamic, dynamic> Property = GetPropertyOfDynamic;

//	//private static dynamic GetPropertyOfDynamic(dynamic dyn)
//	//{
//	//	//string select = String.Empty;
//	//	//////foreach (var p in Activator.CreateInstance(typeof(T)).GetType().GetProperties())
//	//	//foreach (var p in dyn.GetType().GetProperties())
//	//	//{
//	//	//	select += p.Name + ",";
//	//	//}
//	//	//select = select.TrimEnd(',');
//	//	////select += " FROM ";

//	//	////sQuery = select + sQuery;

//	//	string test = "BusinessAction";
//	//	return new {test};
//	//}
//}
