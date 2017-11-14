using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using SuretyTrust.Common.Logging;


namespace SuretyTrust.Common.GenericSQL
{
	public partial class GenericSQL
	{


		/// <summary>
		/// GenericSQL Return List of Type T on DbAction Read
		/// </summary>
		/// <typeparam name="T">Model of type T</typeparam>
		/// <param name="databaseAndTableName"> database and table name</param>
		/// <param name="sqlFilterClause"> filter clause where etc.</param>
		/// <param name="lSqlParams">(Optional) default is null</param>
		/// <returns>List of type T objs</returns>
		public List<T> ModelRead<T>(string databaseAndTableName, string sqlFilterClause, List<SqlParameter> lSqlParams = null)
		{
			try
			{
				var sQuery = databaseAndTableName + " " + sqlFilterClause;

				int nonQueryResult;
				return ExecuteModelRead<T>(sQuery, lSqlParams, DbAction.read, out nonQueryResult);
			}
			catch (GenericSQLException ex)
			{
				bool hayInner = ex.InnerException != null;
				if (lSqlParams == null)
					lSqlParams = new List<SqlParameter>(); // can't pass null objects to the logger
				var type = typeof(T);

				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object>{ databaseAndTableName, sqlFilterClause, lSqlParams },
					ErrorMessage = "ExecuteModelRead<" + type.ToString() + "> FAILED -> Error_Info: " + ex.ErrorInfo + " inner message: " + (hayInner ? ex.InnerException.Message : "N/A"),
					ErrorException = ex
				});
			}
			catch (Exception ex)
			{
				bool hayInner = ex.InnerException != null;
				if (lSqlParams == null)
					lSqlParams = new List<SqlParameter>(); 
				var type = typeof(T);

				GenericSQLException gnSqlException = new GenericSQLException("ExecuteModelRead<" + type.ToString() + "> FAILED", ex);

				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object> { databaseAndTableName, sqlFilterClause, lSqlParams },
					ErrorMessage = "ExecuteModelRead<" + type.ToString() + "> FAILED -> Error_Info: " + gnSqlException.ErrorInfo + " inner message: " + (hayInner ? gnSqlException.InnerException.Message : "N/A"),
					ErrorException = gnSqlException
				});
			}

			return null;
		}

		private List<T> ExecuteModelRead<T>(string sQuery, List<SqlParameter> lSqlParams, DbAction action, out int nonQueryResult)
		{
			SqlCommand db;
			SqlDataReader reader = null;
			List<T> resultSet = null;
			object[] row = null;
			nonQueryResult = 0;

			Connection = new SqlConnection(_connectString);

			if (Connection.State == ConnectionState.Closed)
				Connection.Open();

			/////////// BUILD SQL STATEMENT FROM OBJ PROPERTIES ////////

			if (!sQuery.ToLower().Contains("select"))
			{
				string select = "SELECT ";
				foreach (var p in (Activator.CreateInstance(typeof(T))).GetType().GetProperties())
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
					resultSet = new List<T>();
					var arrayList = new ArrayList();

					while (reader.Read())
					{
						row = AutoColumns ? new Object[reader.FieldCount] : new Object[_columnNum];

						reader.GetValues(row);

						var objT = Activator.CreateInstance(typeof(T));
						Type typeT = typeof(T);
						Type pType = null;
						string pName;
						int i = 0;

						var objT_Properties = objT.GetType().GetProperties().ToList();

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
										if (row[j] != DBNull.Value)
										{

											Type t = Nullable.GetUnderlyingType(pType) ?? pType;
											object safeValue = (row[j] == null) ? null : Convert.ChangeType(row[j], t);

											objT_Properties[k].SetValue(objT, safeValue, null);
										}
										else
										{
											objT_Properties[k].SetValue(objT, null, null);
										}

										// remove this property from collection since it has already been used
										objT_Properties.RemoveAt(k);

									}
								}
								catch (Exception ex)
								{
									// !! LOGGING HAPPENS IN THE WRAPPER !!
									GenericSQLException a = new GenericSQLException("Fill objT of type " + typeT.ToString() + " failed.", ex);

									a.ErrorInfo = "ConversionError: Error when converting value -> " + row[j] + " <- to -> " + pType.ToString() +
												  " | RowCount VS typeT properties count: RowCount = " + row.Length + " properties count = " + objT.GetType().GetProperties().Length +
												  " | sQuery: SQL query = " + sQuery +
												  " | ExceptionMsg: " + ex.Message + " InnerException " + (ex.InnerException != null ? ex.InnerException.ToString() : "N/A");

									throw a;
								}
							}
						}

						arrayList.Add(objT); // have to add to arraylist; it blows up trying to add to list<T>

						row = null;
					}

					resultSet = arrayList.Cast<T>().ToList();

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

			return resultSet;
		}



	}
}
