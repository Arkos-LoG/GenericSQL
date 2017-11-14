using System;
//using System.Collections;
using System.Collections.Generic;
//using System.Configuration;
//using System.Data;
using System.Data.SqlClient;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Web.UI;
//using System.Web.UI.WebControls.Expressions;
using SuretyTrust.Common.GenericSQL.GenericSqlServices;
using SuretyTrust.Common.Logging;

namespace SuretyTrust.Common.GenericSQL
{
	public partial class GenericSQL
	{


		public int SchemaUpdate(string databaseAndTableName, GnSqlUpdateCollection updateCollection, GnSqlConditionsCollection conditionsCollection, string filterCondtionString = null, List<SqlParameter> lSqlParams = null)
		{

			if (String.IsNullOrWhiteSpace(databaseAndTableName))
			{
				throw new ArgumentException("databaseAndTableName cannot be empty");
			}																																							

			if (updateCollection == null || updateCollection.Count == 0)
			{
				throw new ArgumentException("updateCollection null or count is zero");
			}

			if ((conditionsCollection == null || conditionsCollection.Count == 0) && String.IsNullOrWhiteSpace(filterCondtionString)) 
			{
				throw new ArgumentException("Either 1 condition in conditionsCollection is required OR filterConditionString cannot be empty");
			}

			int result = 0;

			try
			{
				result = ExecuteSchemaUpdate(databaseAndTableName, updateCollection, conditionsCollection, filterCondtionString, lSqlParams);
			}
			catch (GenericSQLException ex)
			{
				bool hayInner = ex.InnerException != null;
				if (lSqlParams == null)
					lSqlParams = new List<SqlParameter>(); // can't pass null objects to the logger

				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object> { updateCollection, conditionsCollection, filterCondtionString ?? " filterCondtionString N/A ", lSqlParams },
					ErrorMessage = "ExecuteSchemaUpdate FAILED -> Error_Info: " + ex.ErrorInfo + " inner message: " + (hayInner ? ex.InnerException.Message : "N/A"),
					ErrorException = ex
				});

			}
			catch (Exception ex)
			{
				bool hayInner = ex.InnerException != null;
				if (lSqlParams == null)
					lSqlParams = new List<SqlParameter>(); // can't pass null objects to the logger

				GenericSQLException gnSqlException = new GenericSQLException("ExecuteSchemaUpdate FAILED", ex);

				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object> { updateCollection, conditionsCollection, filterCondtionString ?? " filterCondtionString N/A ", lSqlParams },
					ErrorMessage = "ExecuteSchemaUpdate FAILED -> Error_Info: " + gnSqlException.ErrorInfo + " inner message: " + (hayInner ? ex.InnerException.Message : "N/A"),
					ErrorException = gnSqlException
				});

			}

			return result;
		}


		private int ExecuteSchemaUpdate(string databaseAndTableName, GnSqlUpdateCollection updateCollection, GnSqlConditionsCollection conditionsCollection, 
									    string filterCondtionString = null, List<SqlParameter> lSqlParams = null)
		{

			// todo=== code for unique column Primery/Foreign key etc. 
			// todo=== don't allow sql key words like delete truncate drop insert etc.
			// todo=== verify if this has to happen in other places: 	else if (columnNameAndType[key.FieldName] == typeof(bool)) filterConditions += conditionPart1 + " = " + (Convert.ToBoolean(conditionsCollection.BaseDictionary[item.Key as GnSqlConditionsCollection.FieldCondition]) ? "1" : "0") + ",";
			// todo=== use object instead of string for values
			// todo=== make schemaInsert
			// todo+++ make it so you can pass any table name per conversation with William

			//////////////////////////////////////////////////////////////////////////////////////////////
			//   updateFieldsWithValues	 Section
			//////////////////////////////////////////////////////////////////////////////////////////////

			var columnNameAndType = VerifyDataAgainstTableSchema(updateCollection.BaseDictionary, databaseAndTableName);
			string updateFieldsWithValues = String.Empty;
			string currentKey = String.Empty;

			try
			{
				foreach (var key in updateCollection.Keys)
				{
					currentKey = key.ToString();  
					if (columnNameAndType[key.ToString()] == typeof(string) || columnNameAndType[key.ToString()] == typeof(DateTime))
						updateFieldsWithValues += key + " = '" + updateCollection[key.ToString()] + "',";
					else if (columnNameAndType[key.ToString()] == typeof (bool))
					{
						string value;
						if (updateCollection[key.ToString()] == "0" || updateCollection[key.ToString()] == "1") 
						{
							value = updateCollection[key.ToString()] == "0" ? "false" : "true";
						}
						else
						{
							value = updateCollection[key.ToString()];
						}

						updateFieldsWithValues += key + " = " + (Convert.ToBoolean(value) ? "1" : "0") + ","; //updateFieldsWithValues += key + " = " + (Convert.ToBoolean(updateCollection[key.ToString()]) ? "1" : "0") + ",";
					}
					else
						updateFieldsWithValues += key + " = " + updateCollection[key.ToString()] + ",";
				}
			}
			catch (Exception ex)
			{
			
				GenericSQLException gnSqlException = new GenericSQLException("Failure building Sql updateFieldsWithValues String", ex);
				gnSqlException.ErrorInfo = "Error most likely caused by given key -> " + currentKey + " or item.Key not in given dictionary";

				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object> { columnNameAndType, databaseAndTableName },
					ErrorMessage = "Failure building Sql updateFieldsWithValues String -> Error_Info: " + gnSqlException.ErrorInfo,
					ErrorException = gnSqlException
				});

			}

			//////////////////////////////////////////////////////////////////////////////////////////////
			//   Filter Conditions Section
			//////////////////////////////////////////////////////////////////////////////////////////////
			
			columnNameAndType.Clear();
			columnNameAndType = VerifyDataAgainstTableSchema(conditionsCollection.KeyValuePairDictionary, databaseAndTableName);

			string filterConditions = String.Empty;

			if (String.IsNullOrWhiteSpace(filterCondtionString))
			{
				bool firstTime = true;
				string currentFilterKey = String.Empty;
				foreach (var item in conditionsCollection.UtilityList)
				{
					try
					{
						var key = item.Key as GnSqlConditionsCollection.FieldCondition;
						string conditionPart1 = (firstTime ? String.Empty : key.AndOr) + " " + key.FieldName + " " + key.SqlOperator;
						currentFilterKey = key.FieldName;

						if (columnNameAndType[key.FieldName] == typeof(string) || columnNameAndType[key.FieldName] == typeof(DateTime))  // was thinking about NOT SUPPORTING EXPRESSION HERE BECAUSE OF THE QUOTES NEED FOR THESE DATATYPES CHECKING FOR THAT IN THE BASESERVICES CLASS
							filterConditions += conditionPart1 + " '" + conditionsCollection.BaseDictionary[item.Key as GnSqlConditionsCollection.FieldCondition] + "' ";  // add quotes in for strings and dates
						else if (columnNameAndType[key.FieldName] == typeof(bool))
							filterConditions += conditionPart1 + " " + (Convert.ToBoolean(conditionsCollection.BaseDictionary[item.Key as GnSqlConditionsCollection.FieldCondition]) ? "1" : "0") + " ";
						else
							filterConditions += conditionPart1 + " " + conditionsCollection.BaseDictionary[item.Key as GnSqlConditionsCollection.FieldCondition] + " ";
					}
					catch (Exception ex)
					{
						GenericSQLException gnSqlException = new GenericSQLException("Failure building Sql FilterCondition String", ex);
						gnSqlException.ErrorInfo = "Error most likely caused by given key -> " + currentFilterKey + " or item.Key not in given dictionary";

						throw _logger.LogErrorWithOptions(new ErrorOptions
						{
							Arguments = new List<object> { item, columnNameAndType, filterConditions, databaseAndTableName },
							ErrorMessage = "Failure building Sql FilterCondition String -> Error_Info: " + gnSqlException.ErrorInfo,
							ErrorException = gnSqlException
						});
					}

					firstTime = false;
				}
			}
			else
			{
				filterConditions = filterCondtionString.ToLower().Replace("where", String.Empty);
			} 

			string sQuery = ("UPDATE " + databaseAndTableName + " SET " + updateFieldsWithValues.TrimEnd(',') + " WHERE " + filterConditions);  // NOTE: data checking conditions above force at least 1 condition, so WHERE can stay and not have to be taken out when no conditions


			return ExecuteNonQuery(sQuery, lSqlParams);

		}




	}
}






//	PROTO TYPING CODE BELOW - KEEP UNTIL FINAL VERSION OF THIS HAS BEEN CREATED





// methods create the query then calls this

//string sQuery = "select * from " + databaseAndTableName;

//SqlCommand db;
//SqlDataReader reader = null;

//Connection = (SqlConnection)(_isAmtrustDB ? DbConnections["Amtrust"] : DbConnections["Bond"]);

//if (Connection == null)
//	ConnectToDb();

//if (Connection.State == ConnectionState.Closed)
//{

//IEnumerable<DataRow> schemaCollection;






//}


//var schemaTable = GetSchemaTable(databaseAndTableName);
//IEnumerable<DataRow> schemaCollection = (schemaTable.Rows).Cast<DataRow>();




//var schemaCollection = GetSchemaCollection(databaseAndTableName);



//foreach (var e in schemaCollection)
//{
//	// this link was key to getting this to work http://stackoverflow.com/questions/5737840/whats-the-difference-between-system-type-and-system-runtimetype-in-c 
//	Type runtimeType = e.ItemArray[12].GetType();
//	PropertyInfo propInfo = runtimeType.GetProperty("UnderlyingSystemType");
//	Type type = (Type)propInfo.GetValue(e.ItemArray[12], null);

//	objT_Properties.Add(e.ItemArray[0].ToString(), type);
//}

//const int COLUMN_NAME = 0;
//const int DATA_TYPE = 12;
//const int ALLOW_NULLS = 13;

//var columnDataType = new Dictionary<string, Type>();

////
//// verifiy column name and value against the table schema
////
//foreach (var key in updateCollection.Keys)
//{

//	string fieldName = key.ToString();
//	string value = updateCollection[key.ToString()];
//	bool bFoundIt = false;

//	foreach (var e in schemaCollection)
//	{
//		// this link was key to getting this to work http://stackoverflow.com/questions/5737840/whats-the-difference-between-system-type-and-system-runtimetype-in-c 
//		Type runtimeType = e.ItemArray[DATA_TYPE].GetType();
//		PropertyInfo propInfo = runtimeType.GetProperty("UnderlyingSystemType");

//		Type type = (Type)propInfo.GetValue(e.ItemArray[DATA_TYPE], null);
//		string columnName = e.ItemArray[COLUMN_NAME].ToString();

//		if (columnName.ToLower().Equals(fieldName.ToLower()))
//		{
//			bFoundIt = true;

//			if (value != null && !value.ToLower().Contains("null"))
//			{
//				try
//				{
//					Type t = Nullable.GetUnderlyingType(type) ?? type;
//					object safeValue = Convert.ChangeType(value, t);
//				}
//				catch (Exception ex)
//				{
//					GenericSQLException a = new GenericSQLException("Value in wrong format for sql data type", ex);

//					a.ErrorInfo = "ConversionError: Error when converting value -> " + value + " <- to -> " + type.ToString() + " for column name " + columnName +
//								  " | ExceptionMsg: " + ex.Message + " InnerException " +
//								  (ex.InnerException != null ? ex.InnerException.ToString() : "N/A");

//					throw a;
//				}
//			}
//			else
//			{
//				if (Convert.ToBoolean(e.ItemArray[ALLOW_NULLS]) == false)  // e.ItemArray[13] is the does allow nulls? column
//					throw new Exception("Column Name " + columnName + " does not allow nulls");
//			}

//			columnDataType.Add(columnName, type);

//			break;
//		}		
//	}

//	if (!bFoundIt)
//	{
//		GenericSQLException a = new GenericSQLException("Could not find " + fieldName + " in " + databaseAndTableName);
//		a.ErrorInfo = "Could not find " + fieldName + " in " + databaseAndTableName;

//		throw a;
//	}

//}






//foreach (var key in conditionsCollection.Keys)
//{
//	try
//	{
//		var k = key as GnSqlConditionsCollection.FieldCondition;
//		string conditionPart1 = k.AndOr + " " + k.FieldName + " " + k.SqlWord;

//		if (columnNameAndType[k.FieldName] == typeof(string) || columnNameAndType[k.FieldName] == typeof(DateTime))  // was thinking about NOT SUPPORTING EXPRESSION HERE BECAUSE OF THE QUOTES NEED FOR THESE DATATYPES CHECKING FOR THAT IN THE BASESERVICES CLASS
//			filterConditions += conditionPart1 + " '" + conditionsCollection.BaseDictionary[key as GnSqlConditionsCollection.FieldCondition] + "' ";  // add quotes in for strings and dates
//		else
//			filterConditions += conditionPart1 + " " + conditionsCollection.BaseDictionary[key as GnSqlConditionsCollection.FieldCondition] + " ";
//	}
//	catch (Exception ex)
//	{
//		GenericSQLException a = new GenericSQLException("Failure building Sql FilterCondition String", ex);
//		//a.ErrorInfo = "This is most like because ;

//		throw a;
//	}
//}




	//if (columnNameAndType[(key as GnSqlConditionsCollection.FieldCondition).FieldName] == typeof(string) || columnNameAndType[(key as GnSqlConditionsCollection.FieldCondition).FieldName] == typeof(DateTime))
	//						filterConditions += key.ToString().Replace('^', ' ') + " '" + conditionsCollection[key.ToString()] + "' ";  // add quotes in for strings and dates
	//					else
	//						filterConditions += key.ToString().Replace('^', ' ') + " " + conditionsCollection[key.ToString()] + " ";






//db = new SqlCommand(sQuery, Connection);



//if (action == DbAction.read)
//{
//	reader = db.ExecuteReader();

//	//IEnumerable<DataRow> schemaCollection = ((reader.GetSchemaTable()).Rows).Cast<DataRow>();

//	if (reader.HasRows)
//	{
//		returnList = new List<Dictionary<string, GnSqlRowItem>>();

//		while (reader.Read())
//		{
//			row = AutoColumns ? new Object[reader.FieldCount] : new Object[_columnNum];

//			reader.GetValues(row);

//			//Type typeT = dyn.GetType();
//			Type pType = null;
//			string pName;
//			int i = 0;

//			var objT_Properties = new Dictionary<string, Type>();

//			// fill objT_Properties for dynamic it's foreach (var p in dyn.GetType().GetProperties())
//			foreach (var e in schemaCollection)
//			{
//				// this link was key to getting this to work http://stackoverflow.com/questions/5737840/whats-the-difference-between-system-type-and-system-runtimetype-in-c 
//				Type runtimeType = e.ItemArray[12].GetType();
//				PropertyInfo propInfo = runtimeType.GetProperty("UnderlyingSystemType");
//				Type type = (Type)propInfo.GetValue(e.ItemArray[12], null);

//				objT_Properties.Add(e.ItemArray[0].ToString(), type);
//			}

//			var dict = new Dictionary<string, GnSqlRowItem>();

//			int columnCount = AutoColumns ? reader.FieldCount : _columnNum;

//			for (int j = columnCount - 1; j >= 0; j--)
//			{

//				// if following count is zero then all setvalues for this row has been completed, move on to next read
//				if (objT_Properties.Count == 0)
//					break;

//				string fieldName = reader.GetName(j);

//				for (int k = objT_Properties.Count - 1; k >= 0; k--)
//				{
//					pType = objT_Properties.ElementAt(k).Value;
//					pName = objT_Properties.ElementAt(k).Key;

//					try
//					{
//						if (pName.ToLower().Equals(fieldName.ToLower()))
//						{
//							var objT = new GnSqlRowItem();

//							if (row[j] != DBNull.Value)
//							{
//								Type t = Nullable.GetUnderlyingType(pType) ?? pType;
//								object safeValue = (row[j] == null) ? null : Convert.ChangeType(row[j], t);

//								objT.FieldName = pName;
//								objT.PropertyType = pType;
//								objT.Value = safeValue;
//							}
//							else
//							{
//								objT.FieldName = pName;
//								objT.PropertyType = pType;
//								objT.Value = null;
//							}

//							// remove this property from collection since it has already been used
//							objT_Properties.Remove(pName);

//							dict.Add(objT.FieldName, objT);
//						}
//					}
//					catch (Exception ex)
//					{
//						// !! LOGGING HAPPENS IN THE WRAPPER !!
//						GenericSQLException a = new GenericSQLException("SchemaRead Failure see ErrorInfo for more information", ex);

//						a.ErrorInfo = "ConversionError: Error when converting value -> " + row[j] + " <- to -> " + pType.ToString() +
//									  " | sQuery: SQL query = " + sQuery +
//									  " | ExceptionMsg: " + ex.Message + " InnerException " + (ex.InnerException != null ? ex.InnerException.ToString() : "N/A");

//						throw a;
//					}
//				}
//			}

//			returnList.Add(dict);
//			row = null;
//		}
//	}

//	reader.Dispose();
//	reader = null;
//}
//else
//{
//	throw new NotSupportedException("only dbaction read supported");
//}

//if (Connection.State == ConnectionState.Open)
//	Connection.Close();

//return returnList;