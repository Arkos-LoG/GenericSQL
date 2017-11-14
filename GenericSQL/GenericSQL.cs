using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using log4net;
using SuretyTrust.Common.Logging;


namespace SuretyTrust.Common.GenericSQL
{

	public class GnSqlRowItem
	{
		public string FieldName { get; set; }
		public Type PropertyType { get; set; }
		public dynamic Value { get; set; }
	}

	public class GnSqlDynamicResultSet
	{
		public List<GnSqlRowItem> Items { get; set; }

		public GnSqlDynamicResultSet()
		{
			Items = new List<GnSqlRowItem>();
		}
	}

	public partial class GenericSQL
	{

		private int _columnNum;
		private Boolean _isAmtrustDB;

		private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private string _connectString;

		public SqlConnection Connection { get; set; }

		public int ColumnNum // don't make this an automatic  ; this is the amount of columns you want returned from a row result
		{
			get
			{
				return _columnNum;
			}
			set
			{
				_columnNum = value;
			}
		}

		public Boolean IsAmtrustDB // don't make this an automatic
		{
			get
			{
				return _isAmtrustDB;
			}
			set
			{
				_isAmtrustDB = value;
			}
		}

		public bool AutoColumns { get; set; } // set this true if you want it to try to return all columns in Reader.FieldCount

		public enum DbAction
		{
			read,
			insert,
			update,
			delete
		};

		public enum WhereAndOr
		{
			AND,
			OR
		};

		public enum Context
		{
			AmtrustContext,
			BusinessCentralContext,
			ISORatingContext,
			SuretyDBContext,
			SuretyCentralContext,
			XBondDBContext
		}

		public class UpdateObj
		{
			public WhereAndOr WhereAndOr { get; set; }
			public Dictionary<string, string> UpdateFieldsWithValues { get; set; }
			public Dictionary<string, string> WhereFieldsWithValuesEqual { get; set; }
		}


		/// <summary>
		/// GenericSQL Constructor
		/// </summary>
		/// <param name="context">default is AmtrustContext</param>
		/// <param name="bAutoColumns">default is true</param>
		/// <returns></returns>
		public GenericSQL(Context context = Context.AmtrustContext, bool bAutoColumns = true)
		{
			_columnNum = 1;// default return only 1 column for when autocolumns is turned off
			AutoColumns = bAutoColumns;
			SetConnectionString(context);
		}

		private void SetConnectionString(Context context)
		{
			switch (context)
			{
				case Context.AmtrustContext:
					_connectString = ConfigurationManager.ConnectionStrings["AmtrustContext"].ConnectionString;
					break;
				case Context.BusinessCentralContext:
					_connectString = ConfigurationManager.ConnectionStrings["BusinessCentralContext"].ConnectionString;
					break;
				case Context.ISORatingContext:
					_connectString = ConfigurationManager.ConnectionStrings["ISORatingContext"].ConnectionString;
					break;
				case Context.SuretyCentralContext:
					_connectString = ConfigurationManager.ConnectionStrings["SuretyCentralContext"].ConnectionString;
					break;
				case Context.SuretyDBContext:
					_connectString = ConfigurationManager.ConnectionStrings["SuretyDBContext"].ConnectionString;
					break;
				case Context.XBondDBContext:
					_connectString = ConfigurationManager.ConnectionStrings["XBondDBContext"].ConnectionString;
					break;
			}
		}


		public object Execute<T>(T model, string fullDBtableName, DbAction action, UpdateObj update = null, string sQuery = null, List<SqlParameter> lSqlParams = null)
		{
			int nonQueryResult;
			string query = String.Empty;
			string insertColumns = String.Empty;
			string insertValues = String.Empty;
			string updateFieldsWithValues = String.Empty;
			string whereFieldsWithValuesEqual = String.Empty;

			switch (action)
			{
				case DbAction.insert:

					try
					{
						foreach (var p in model.GetType().GetProperties())
						{
							//
							// -- LOOK FOR IgnoreOnInsert Attribute --
							//
							bool flag = false;
							foreach (var attribute in p.GetCustomAttributes(false))
							{
								if (attribute is GnSqlIgnoreOnInsertAttribute)
								{
									flag = true;
									break;
								}
							}
							if (flag) continue;

							/////////////////////////////////////////////////////

							
							object value;
							try
							{
								value = p.GetValue(model);
								if (value == null)
									continue;
							}
							catch (NullReferenceException)
							{
								continue; // if null then move on to next kvpair //value = "null";
							}

							insertColumns += p.Name + ",";

							if (p.PropertyType.FullName == "System.String" || p.PropertyType.FullName == "System.DateTime" || p.PropertyType.FullName == "System.Guid") // todo--- switch statement to deal with other types
								insertValues += "'" + value + "',";
							else if (p.PropertyType.FullName == "System.Boolean")
								insertValues += (value.ToString().ToLower() == "true" ? "1" : "0") + ",";
							else
								insertValues += value + ",";
						}

						query = "INSERT INTO " + fullDBtableName + " (" + insertColumns.TrimEnd(',') + ") VALUES (" + insertValues.TrimEnd(',') + ")";    //	INSERT INTO table_name (column1,column2,column3,...) VALUES (value1,value2,value3,...);

						Execute(query, lSqlParams, action, out nonQueryResult);
						return nonQueryResult;
					}
					catch (Exception ex)
					{

						if (lSqlParams == null)
							lSqlParams = new List<SqlParameter>(); // can't pass null objects to the logger
						var type = typeof(T);

						GenericSQLException gnSqlException = new GenericSQLException("", ex);
						//throw _logger.LogError("Execute<" + type.ToString() + "> DbAction.insert FAILED -> Error_Info: " + gnSqlException.ErrorInfo, gnSqlException, model, fullDBtableName, update, sQuery, lSqlParams);

						throw _logger.LogErrorWithOptions(new ErrorOptions
						{
							Arguments = new List<object>{ model, fullDBtableName, update, sQuery, lSqlParams },
							ErrorMessage = "Execute<" + type.ToString() + "> DbAction.insert FAILED -> Error_Info: " + gnSqlException.ErrorInfo,
							ErrorException = gnSqlException
						});

					}

	
				//----------------------------------------------------------------------------------------------------------------------------------------
				case DbAction.update:
				//----------------------------------------------------------------------------------------------------------------------------------------

					try
					{
						if (update == null) { throw new ArgumentNullException("ERROR: updateObj is null for DbAction.update"); }

						foreach (var u in update.UpdateFieldsWithValues)
						{
							var propertyType = (model.GetType().GetProperty(u.Key)).PropertyType.FullName;

							if (propertyType == "System.String" || propertyType == "System.DateTime") // todo--- switch statement to deal with other types
								updateFieldsWithValues += u.Key + " = '" + u.Value + "',";
							else
								updateFieldsWithValues += u.Key + " = " + u.Value + ",";
						}

						foreach (var w in update.WhereFieldsWithValuesEqual)
						{
							var propertyType = (model.GetType().GetProperty(w.Key)).PropertyType.FullName;

							if (propertyType == "System.String" || propertyType == "System.DateTime") // todo--- switch statement to deal with other types
								whereFieldsWithValuesEqual += w.Key + " = " + w.Value + " " + update.WhereAndOr + " ";
							else
								whereFieldsWithValuesEqual += w.Key + " = '" + w.Value + "' " + update.WhereAndOr + " ";
						}

						var split = whereFieldsWithValuesEqual.Split(' ');
						whereFieldsWithValuesEqual = String.Empty;

						for (int i = 0; i < split.Length - 2; i++) { whereFieldsWithValuesEqual += split[i] + " "; } // take off the last and/or

						query = ("UPDATE " + fullDBtableName + " SET " + updateFieldsWithValues.TrimEnd(',') + " WHERE " + whereFieldsWithValuesEqual);

						Execute(query, lSqlParams, action, out nonQueryResult);
						return nonQueryResult;
					}
					catch (Exception ex)
					{
						if (lSqlParams == null)
							lSqlParams = new List<SqlParameter>(); // can't pass null objects to the logger
						var type = typeof(T);

						GenericSQLException gnSqlException = new GenericSQLException("", ex);
						//throw _logger.LogError("Execute<" + type.ToString() + "> DbAction.update FAILED -> Error_Info: " + gnSqlException.ErrorInfo, gnSqlException, model, fullDBtableName, update, sQuery, lSqlParams);

						throw _logger.LogErrorWithOptions(new ErrorOptions
						{
							Arguments = new List<object>{ model, fullDBtableName, update, sQuery, lSqlParams },
							ErrorMessage = "Execute<" + type.ToString() + "> DbAction.update FAILED -> Error_Info: " + gnSqlException.ErrorInfo,
							ErrorException = gnSqlException

						});

					}
				

				//----------------------------------------------------------------------------------------------------------------------------------------
				// todo--- case DbAction.Remove/Delete
				//----------------------------------------------------------------------------------------------------------------------------------------

			}

			//return Execute(sQuery, lSqlParams, Action);
			return new List<object>();

		}

		public List<Object> Execute(String sQuery, DbAction action, List<SqlParameter> lSqlParams = null)
		{
			int nonQueryResult;
			try
			{
				return Execute(sQuery, lSqlParams, action, out nonQueryResult);
			}
			catch (Exception ex)
			{

				GenericSQLException gnSqlException = new GenericSQLException("List<Object> Execute FAILED", ex);
				//throw _logger.LogError("List<Object> Execute FAILED: " + gnSqlException.ErrorInfo, gnSqlException, sQuery, action, lSqlParams);

				if (lSqlParams == null)
					lSqlParams = new List<SqlParameter>(); 

				gnSqlException.ErrorInfo = "List<Object> Execute FAILED";
				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object>{ sQuery, action, lSqlParams },
					ErrorException = gnSqlException
				});

			}
		}

		public int ExecuteNonQuery(String sQuery, DbAction action, List<SqlParameter> lSqlParams = null)
		{
			int nonQueryResult;
			try
			{
				Execute(sQuery, lSqlParams, action, out nonQueryResult);
				return nonQueryResult;	
			}
			catch (Exception ex)
			{

				GenericSQLException gnSqlException = new GenericSQLException("int ExecuteNonQuery FAILED", ex);

				if (lSqlParams == null)
					lSqlParams = new List<SqlParameter>();

				gnSqlException.ErrorInfo = "int ExecuteNonQuery FAILED";
				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object> { sQuery, action, lSqlParams },
					ErrorException = gnSqlException
				});

			}
		}

		public List<Object> Execute(String sQuery, List<SqlParameter> lSqlParams, DbAction action, out int nonQueryResult)
		{
			SqlCommand db;
			SqlDataReader reader = null;
			List<object> resultSet = null;
			object[] row = null;
			nonQueryResult = 0;

			Connection = new SqlConnection(_connectString);

			if (Connection.State == ConnectionState.Closed)
				Connection.Open();

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
					resultSet = new List<object>();

					while (reader.Read())
					{
						row = AutoColumns ? new Object[reader.FieldCount] : new Object[_columnNum];

						reader.GetValues(row);
						resultSet.Add(row);
						row = null;
					}
				}

				reader.Dispose();
				reader = null;
			}
			else
			{
				nonQueryResult = db.ExecuteNonQuery();
			}

			if (Connection.State == ConnectionState.Open)
				Connection.Close();

			return resultSet;
		}

	}


}


// for reference used in intermediate window for getting what was needed from --> <T> model
//model.GetType().GetProperties()[0].Name;
//model.GetType().GetProperties()[0].GetValue(model)
//model.GetType().GetProperties()[0].PropertyType.FullName  -->  "System.String"