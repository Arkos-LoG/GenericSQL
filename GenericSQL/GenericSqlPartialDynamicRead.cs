using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using SuretyTrust.Common.Logging;


namespace SuretyTrust.Common.GenericSQL
{
	public partial class GenericSQL
	{

		/// <summary>
		/// GenericSQL Action (Read) Return List of Dictionary<string, GnSqlRowItem>
		/// </summary>
		/// <param name="dyn">anonomous type of a model</param>
		/// <param name="databaseAndTableName"> database and table name</param>
		/// <param name="sqlFilterClause"> filter clause where etc.</param>
		/// <param name="lSqlParams">(Optional) default is null</param>
		/// <returns>List of Dictionary<string, GnSqlRowItem></returns>
		public List<Dictionary<string, GnSqlRowItem>> DynamicRead(dynamic dyn, string databaseAndTableName, string sqlFilterClause, List<SqlParameter> lSqlParams = null)
		{
			List<Dictionary<string, GnSqlRowItem>> result = null;

			try
			{
				var sQuery = databaseAndTableName + " " + sqlFilterClause;

				result = ExecuteDynamicRead(dyn, sQuery, DbAction.read, lSqlParams);
			}
			catch (GenericSQLException ex)
			{
				string dynFields = String.Empty;
				foreach (var p in dyn.GetType().GetProperties())
				{
					dynFields += p.Name + ",";
				}

				bool hayInner = ex.InnerException != null;
				if (lSqlParams == null)
					lSqlParams = new List<SqlParameter>(); // can't pass null objects to the logger

				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object> { dynFields, databaseAndTableName, sqlFilterClause, lSqlParams },
					ErrorMessage = "ExecuteDynamicRead FAILED -> Error_Info: " + ex.ErrorInfo + " inner message: " + (hayInner ? ex.InnerException.Message : "N/A"),
					ErrorException = ex
				});
			}
			catch (Exception ex)
			{
				string dynFields = String.Empty;
				foreach (var p in dyn.GetType().GetProperties())
				{
					dynFields += p.Name + ",";
				}

				GenericSQLException gnSqlException = new GenericSQLException("ExecuteDynamicRead FAILED: dynamic fields", ex);
				gnSqlException.ErrorInfo = "ExecuteDynamicRead FAILED: dynamic fields -> " + dynFields;

				bool hayInner = ex.InnerException != null;
				if (lSqlParams == null)
					lSqlParams = new List<SqlParameter>(); // can't pass null objects to the logger

				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object> { dynFields, databaseAndTableName, sqlFilterClause, lSqlParams },
					ErrorMessage = "ExecuteDynamicRead FAILED: dynamic fields -> Error_Info: " + gnSqlException.ErrorInfo + " inner message: " + (hayInner ? gnSqlException.InnerException.Message : "N/A"),
					ErrorException = gnSqlException
				});

			}


			return result;
		}

		private List<Dictionary<string, GnSqlRowItem>> ExecuteDynamicRead(dynamic dyn, String sQuery, DbAction action = DbAction.read, List<SqlParameter> lSqlParams = null)
		{
			SqlCommand db;
			SqlDataReader reader = null;
			List<Dictionary<string, GnSqlRowItem>> returnList = null;
			object[] row = null;

			Connection = new SqlConnection(_connectString);

			if (Connection.State == ConnectionState.Closed)
				Connection.Open();

			/////////// BUILD SQL STATEMENT FROM OBJ PROPERTIES ////////

			if (!sQuery.ToLower().Contains("select"))
			{
				string select = "SELECT ";
				foreach (var p in dyn.GetType().GetProperties())
				{
					select += p.Name + ",";
				}
				select = select.TrimEnd(',');
				select += " FROM ";

				sQuery = select + sQuery;
			}

			////////////////////////////////////////////////////////////
	
			db = new SqlCommand(sQuery, Connection);

			if (lSqlParams != null)
			{
				foreach (SqlParameter deParam in lSqlParams)
				{
					db.Parameters.Add(deParam);
				}
			}

			if (action == DbAction.read)
			{
				reader = db.ExecuteReader();

				if (reader.HasRows)
				{
					returnList = new List<Dictionary<string, GnSqlRowItem>>();

					while (reader.Read())
					{
						row = AutoColumns ? new Object[reader.FieldCount] : new Object[_columnNum];

						reader.GetValues(row);

						Type typeT = dyn.GetType();
						Type pType = null;
						string pName;
						int i = 0;

						var objT_Properties = new List<dynamic>();
						foreach (var p in dyn.GetType().GetProperties())
						{
							objT_Properties.Add(p);
						}

						var dict = new Dictionary<string, GnSqlRowItem>();

						int columnCount = AutoColumns ? reader.FieldCount : _columnNum;

						for (int j = columnCount - 1; j >= 0; j--)
						{

							// if following count is zero then all setvalues for this row has been completed, move on to next read
							if (objT_Properties.Count == 0)
								break;

							string fieldName = reader.GetName(j);

							for (int k = objT_Properties.Count - 1; k >= 0; k--)
							{
								pType = objT_Properties[k].PropertyType;
								pName = objT_Properties[k].Name;
					
								try
								{
									if (pName.ToLower().Equals(fieldName.ToLower()))
									{
										var objT = new GnSqlRowItem();

										if (row[j] != DBNull.Value)
										{
											Type t = Nullable.GetUnderlyingType(pType) ?? pType;
											object safeValue = (row[j] == null) ? null : Convert.ChangeType(row[j], t);

											objT.FieldName = pName;
											objT.PropertyType = pType;
											objT.Value = safeValue;
										}
										else
										{
											objT.FieldName = pName;
											objT.PropertyType = pType;
											objT.Value = null;
										}

										// remove this property from collection since it has already been used
										objT_Properties.RemoveAt(k);

										dict.Add(objT.FieldName, objT);
									}
								}
								catch (Exception ex)
								{

									// !! LOGGING HAPPENS IN THE WRAPPER !!
									GenericSQLException a = new GenericSQLException("Fill objT of type " + typeT.ToString() + " failed.", ex);

									a.ErrorInfo = "ConversionError: Error when converting value -> " + row[j] + " <- to -> " + pType.ToString() +
												  " | sQuery: SQL query = " + sQuery +
												  " | ExceptionMsg: " + ex.Message + " InnerException " + (ex.InnerException != null ? ex.InnerException.ToString() : "N/A");

									throw a;
								}
							}
						}

						returnList.Add(dict);
						row = null;
					}
				}

				reader.Dispose();
				reader = null;
			}
			else
			{
				throw new NotSupportedException("only dbaction read supported");
			}

			if (Connection.State == ConnectionState.Open)
				Connection.Close();

			return returnList;
		}




	}
}
