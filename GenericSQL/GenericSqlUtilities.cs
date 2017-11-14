using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Web.UI;
using SuretyTrust.Common.Logging;

namespace SuretyTrust.Common.GenericSQL
{
	public partial class GenericSQL
	{
		public int ExecuteNonQuery(string fullSqlQuery, List<SqlParameter> lSqlParams = null)
		{
			int result = 0;

			try
			{
				using (var conn = new SqlConnection(_connectString))
				using (var db = new SqlCommand(fullSqlQuery, conn))
				{
					if (lSqlParams != null)
					{
						foreach (SqlParameter deParam in lSqlParams)
						{
							db.Parameters.Add(deParam);
						}
					}

					conn.Open();

					result = db.ExecuteNonQuery();
				}
			}
			catch (Exception ex)
			{
				if (lSqlParams == null)
					lSqlParams = new List<SqlParameter>(); 

				GenericSQLException gnSqlException = new GenericSQLException("ExecuteNonQuery Failed squery = " + fullSqlQuery, ex);

				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object> { fullSqlQuery, _connectString, lSqlParams },
					ErrorMessage = "ExecuteNonQuery Failed squery = " + fullSqlQuery + " -> Error_Info: " + gnSqlException.ErrorInfo,
					ErrorException = gnSqlException
				});
			}

			return result;
		}


		public IEnumerable<DataRow> GetSchemaCollection(string databaseAndTableName)
		{
			try
			{
				using (var conn = new SqlConnection(_connectString))
				using (var db = new SqlCommand("select TOP 10 * from " + databaseAndTableName, conn))
				{
					conn.Open();

					using (SqlDataReader reader = db.ExecuteReader())
					{
						var schemaTable = reader.GetSchemaTable();
						db.Cancel();	// http://stackoverflow.com/questions/19686488/sqldatareader-hangs-on-dispose
						return (schemaTable.Rows).Cast<DataRow>();
					}
				}
			}
			catch (Exception ex)
			{
				GenericSQLException gnSqlException = new GenericSQLException("GetSchemaCollection Failed", ex);

				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object> { databaseAndTableName, _connectString },
					ErrorMessage = "GetSchemaCollection Failed -> Error_Info: " + gnSqlException.ErrorInfo,
					ErrorException = gnSqlException
				});
			}

			return null;
		}

	

		public Dictionary<string, Type> VerifyDataAgainstTableSchema(IDictionary dataToBeVerified, string databaseAndTableName)
		{
			const int COLUMN_NAME = 0;
			const int DATA_TYPE = 12;
			const int ALLOW_NULLS = 13;

			var columnNameAndType = new Dictionary<string, Type>();

			var schemaCollection = GetSchemaCollection(databaseAndTableName);

			foreach (var key in dataToBeVerified.Keys)
			{
				string fieldName = key.ToString();
				string value = dataToBeVerified[key.ToString()] as string;
				bool bFoundIt = false;

				foreach (var e in schemaCollection)
				{
					// this link was key to getting this to work http://stackoverflow.com/questions/5737840/whats-the-difference-between-system-type-and-system-runtimetype-in-c 
					Type runtimeType = e.ItemArray[DATA_TYPE].GetType();
					PropertyInfo propInfo = runtimeType.GetProperty("UnderlyingSystemType");

					Type type = (Type)propInfo.GetValue(e.ItemArray[DATA_TYPE], null);
					string columnName = e.ItemArray[COLUMN_NAME].ToString();

					//////////////////////////////////////////////////////////////////////////
					//	Verify the column exists 
					/////////////////////////////////////////////////////////////////////////
					
					if (columnName.ToLower().Equals(fieldName.ToLower()))
					{
						bFoundIt = true;

						//////////////////////////////////////////////////////////////////////////
						//	 Verify Value Data against Schema Type
						/////////////////////////////////////////////////////////////////////////

						if (!value.Contains(" ") && !value.Contains("%") && !value.Contains("(")) // don't verify is value has spaces, this mostly like means an expression is being used
							if (value != null && !value.ToLower().Contains("null"))
							{
								try
								{
									Type t = Nullable.GetUnderlyingType(type) ?? type;

									if (t.FullName == "System.Boolean" && (value == "0" || value == "1"))
									{
										value = value == "0" ? "false" : "true";
									}

									object safeValue = Convert.ChangeType(value, t);
								}
								catch (Exception ex)
								{
									GenericSQLException gnSqlException = new GenericSQLException("Value in wrong format for sql data type", ex);

									gnSqlException.ErrorInfo = "ConversionError: Error when converting value -> " + value + " <- to -> " + type.ToString() + " for column name " + columnName +
												  " | ExceptionMsg: " + ex.Message + " InnerException " +
												  (ex.InnerException != null ? ex.InnerException.ToString() : "N/A");

									throw gnSqlException;
								}
							}
							else
							{
								if (Convert.ToBoolean(e.ItemArray[ALLOW_NULLS]) == false)  // e.ItemArray[13] is the does allow nulls? column
									throw new GenericSQLException("Column Name " + columnName + " does not allow nulls");
							}

						columnNameAndType.Add(columnName, type);

						break;
					}
				}

				if (!bFoundIt)
				{
					GenericSQLException gnSqlException = new GenericSQLException("Could not find " + fieldName + " in " + databaseAndTableName);
					gnSqlException.ErrorInfo = "Could not find " + fieldName + " in " + databaseAndTableName;

					throw gnSqlException;
				}
			}

			return columnNameAndType;
		}


	}
}





//public class SqlDataPackage 
//{
//	public SqlDataReader Sql_DataReader { get; set; }
//	public SqlConnection Sql_Connection { get; set; }

//	public SqlDataPackage(ref SqlConnection sqlConnection)
//	{
//		Sql_Connection = sqlConnection;
//	}

//}




	//try
	//{
	//	using (var conn = new SqlConnection(_connectString))
	//	using (var db = new SqlCommand(sQuery, conn))
	//	{
	//		if (lSqlParams != null)
	//		{
	//			foreach (SqlParameter param in lSqlParams)
	//			{
	//				db.Parameters.Add(param);
	//			}
	//		}

	//		conn.Open();

	//		using (SqlDataReader reader = db.ExecuteReader())
	//		{
	//			var schemaTable = reader.GetSchemaTable();
	//			db.Cancel();	// http://stackoverflow.com/questions/19686488/sqldatareader-hangs-on-dispose
	//			return (schemaTable.Rows).Cast<DataRow>();
	//		}
		
	//	}

	//}
	//catch (GenericSQLException ex)
	//{
	//	ex.ErrorInfo = "ConnectToDb() FAILED";
	//	throw _logger.LogError(ex, ConfigurationManager.ConnectionStrings["AmtrustContext"].ConnectionString, ConfigurationManager.ConnectionStrings["BusinessCentralContext"].ConnectionString);
	//}






		//public SqlDataReader GetReader(string sQuery, List<SqlParameter> lSqlParams = null)
		//{
		//	SqlCommand db;
		//	SqlDataReader reader = null;

		//	Connection = (SqlConnection)(_isAmtrustDB ? DbConnections["Amtrust"] : DbConnections["Bond"]);

		//	if (Connection == null)
		//		ConnectToDb();

		//	if (Connection.State == ConnectionState.Closed)
		//		Connection.Open();

		//	db = new SqlCommand(sQuery, Connection);

		//	if (lSqlParams != null)
		//	{
		//		foreach (SqlParameter deParam in lSqlParams)
		//		{
		//			db.Parameters.Add(deParam);
		//		}
		//	}

		//	//var sqlDataPackage = new SqlDataPackage(ref connection);

		//	return db.ExecuteReader();
		//}
