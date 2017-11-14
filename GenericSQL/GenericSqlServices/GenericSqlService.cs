using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Data.SqlClient;
using SuretyTrust.Common.Logging;
using log4net;

namespace SuretyTrust.Common.GenericSQL.GenericSqlServices
{
	public class GenericSqlService
	{

		// todo=== optional param for comparison operator <  =  like  g t e l  in (validate list per Will)

		private GnSqlUpdateCollection _columnsToUpdate = new GnSqlUpdateCollection();
		private GnSqlConditionsCollection _FieldConditionsEqual = new GnSqlConditionsCollection();


		public GnSqlUpdateCollection ColumnsToUpdate
		{
			get { return _columnsToUpdate; }
			set { _columnsToUpdate = value;  }
		}

		public GnSqlConditionsCollection FieldConditions
		{
			get { return _FieldConditionsEqual; }
			set { _FieldConditionsEqual = value; }
		}

		public string FilterCondtionString { get; set; }
		public string DatabaseAndTableName { get; set; }

		public List<SqlParameter> SqlParams { get; set; }

		public GenericSqlService(string databaseAndTableName)
		{
			DatabaseAndTableName = databaseAndTableName;
		}

		public virtual int Update()
		{
			return (new GenericSQL()).SchemaUpdate(DatabaseAndTableName, ColumnsToUpdate, FieldConditions, FilterCondtionString, SqlParams); 
		}

	}


	public enum SqlWord
	{
		[StringValue("=")]
		Equals,
		[StringValue("<>")]
		IsNotEqualTo,
		[StringValue(">")]
		IsGreaterThan,
		[StringValue("<")]
		IsLessThan,
		[StringValue(">=")]
		IsGreaterThanOrEqual,
		[StringValue("<=")]
		IsLessThanOrEqual,
		[StringValue("BETWEEN")]
		IsBetween,
		[StringValue("LIKE")]
		LIKE,
		[StringValue("IN")]
		IN,
		[StringValue("AND")]
		AND,
		[StringValue("OR")]
		OR,
		[StringValue("WHERE")]
		WHERE,
		[StringValue("NOT")]
		NOT,
		[StringValue("IS NULL")]
		IsNull

	};


	public class GnSqlUpdateCollection : GnSqlDictionaryBase
	{
		public void AddColumn(string columnName, string value)
		{
			//if (value.IndexOfAny(new char[] { ' ', '%', '(', ')' }) > -1)  // ?? DON'T ALLOW EXPRESSION IF THEY WANT TO DO EXPRESSION THEY USE THE ADHOC STRING
			//{
			//	throw new GenericSQLException("Characters: ' ', '%', '(', ')' are not allowed");
			//}

			try
			{		
				Dictionary.Add(columnName, value);
			}
			catch (Exception ex)
			{
				GenericSQLException gnSqlException = new GenericSQLException("GnSqlUpdateCollection Failed to AddColumn", ex);
				gnSqlException.ErrorInfo = "GnSqlUpdateCollection Failed to AddColumn";

				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object> { columnName, value },
					ErrorMessage = "GnSqlUpdateCollection Failed to AddColumn -> Error_Info: " + gnSqlException.ErrorInfo,
					ErrorException = gnSqlException
				});
			}
		}
	}


	public class GnSqlConditionsCollection : GnSqlDictionaryBase
	{

		public List<KeyValuePair<FieldCondition, string>> UtilityList { get; set; }  // public List<KeyValuePair<FieldCondition, string>> UtilityList { get; set; }

		public GnSqlConditionsCollection()
		{
			UtilityList = new List<KeyValuePair<FieldCondition, string>>();
		}
		
		// todo ok with this changed out ??? public void AddFieldCondition(AndOr andOr, string fieldName, SqlWord sqlOperator2, string value)
		public void AddFieldCondition(SqlWord sqlOperator1, string fieldName, SqlWord sqlOperator2, string value)
		{

			if (sqlOperator2 == SqlWord.LIKE && !value.Contains("%"))
			{
				GenericSQLException gnSqlException = new GenericSQLException("AddFieldCondition Failure: value does not contain % when using LIKE operator");

				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object> { sqlOperator1, fieldName, sqlOperator2, value },
					ErrorMessage = "AddFieldCondition Failure: value does not contain % when using LIKE operator -> Error_Info: " + gnSqlException.ErrorInfo,
					ErrorException = gnSqlException
				});
			}

			try
			{
																							// ? DON'T ALLOW EXPRESSION IF THEY WANT TO DO EXPRESSION THEY USE THE ADHOC STRING
				var operator1 = StringValueAttribute.GetStringValue(sqlOperator1);
				var operator2 = StringValueAttribute.GetStringValue(sqlOperator2);

				var f = new FieldCondition()
				{
					AndOr = operator1, //AndOr = andOr.ToString(),
					FieldName = fieldName,
					SqlOperator = operator2 
				};

				Dictionary.Add(f, value);
				UtilityList.Add(new KeyValuePair<FieldCondition, string>(f, value));// UtilityList.Add(new KeyValuePair<FieldCondition, string>(f, value));

			}
			catch (Exception ex)
			{
				GenericSQLException gnSqlException = new GenericSQLException("GnSqlConditionsCollection Failed to AddField", ex);
				gnSqlException.ErrorInfo = "GnSqlConditionsCollection Failed to AddField";

				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object> { sqlOperator1, fieldName, sqlOperator2, value },
					ErrorMessage = "GnSqlConditionsCollection Failed to AddField -> Error_Info: " + gnSqlException.ErrorInfo,
					ErrorException = gnSqlException
				});
			}
		}

		public IDictionary KeyValuePairDictionary
		{
			get
			{
				var kvpDict = new Dictionary<string, string>();

				foreach (var key in Dictionary.Keys)
				{				
					try
					{
						kvpDict.Add((key as FieldCondition).FieldName, Dictionary[key as FieldCondition].ToString());
					}
					catch (Exception ex)
					{
						GenericSQLException gnSqlException = new GenericSQLException("GnSqlConditionsCollection Failed to AddField to KeyValuePairDictionary", ex);
						gnSqlException.ErrorInfo = "GnSqlConditionsCollection Failed to AddField to KeyValuePairDictionary";

						throw _logger.LogErrorWithOptions(new ErrorOptions
						{
							Arguments = new List<object> { key },
							ErrorMessage = "GnSqlConditionsCollection Failed to AddField to KeyValuePairDictionary -> Error_Info: " + gnSqlException.ErrorInfo,
							ErrorException = gnSqlException
						});
					}
				}

				return kvpDict;
			}
		}

		public List<string> ColumnNames
		{
			get
			{
				var list = new List<string>();

				foreach (var key in Dictionary.Keys)
				{
					list.Add((key as FieldCondition).FieldName);
				}

				return list;
			}
		}


		public class FieldCondition
		{
			public string AndOr { get; set; }
			public string FieldName { get; set; }
			public string SqlOperator { get; set; }
		}
	}


	public class GnSqlDictionaryBase : DictionaryBase // http://msdn.microsoft.com/en-us/library/system.collections.dictionarybase%28v=vs.110%29.aspx
	{
		protected static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public ICollection Values
		{
			get
			{
				return (Dictionary.Values);
			}
		}

		public String this[String key]
		{
			get
			{
				return ((String)Dictionary[key]);
			}
			set
			{
				Dictionary[key] = value;
			}
		}

		public ICollection Keys
		{
			get
			{
				return (Dictionary.Keys);
			}
		}

		public bool Contains(String key)
		{
			return (Dictionary.Contains(key));
		}

		public void Remove(String key)
		{
			Dictionary.Remove(key);
		}

		public IDictionary BaseDictionary 
		{
			get { return Dictionary; }
		}

	}

}



//private GenericSQL.WhereAndOr _whereAndOr = GenericSQL.WhereAndOr.AND;
		

//public GenericSQL.WhereAndOr WhereAndOr 
//{
//	set { _whereAndOr = value; } 
//}



//Dictionary.Add(andOr + "^" + fieldName + "^" + operatorStr, value);

//var splitKey = key.ToString().Split('^');

//public class GnSqlDictionary : Dictionary<string, string>
//{

//	public Dictionary<string, string> dict = new Dictionary<string, string>();

//	public void AddColumn(string columnName, string ValueOrExpression)
//	{
//		dict.Add(columnName,ValueOrExpression);
//	}	
//}