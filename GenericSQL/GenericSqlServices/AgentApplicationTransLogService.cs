using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using SuretyTrust.Common.GenericSQL;
using SuretyTrust.Common.Logging;
using SuretyTrust.Common.Models;
using log4net;

namespace SuretyTrust.Common.GenericSQL.GenericSqlServices
{
	public class AgentApplicationTransLogService : GenericSqlService
	{
		private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public AgentApplicationTransLogService() : base("[GrandCentral].[dbo].[AgentApplicationTransLog]") {}


		public void SetAllToInconsequential(string agentId, string quoteNum)
		{
			//AgentApplicationTransLogService aaTransLogSvc = null;
			try
			{
				//aaTransLogSvc = new AgentApplicationTransLogService();

				ColumnsToUpdate.AddColumn("InconsequentialActivity", "1");
				FieldConditions.AddFieldCondition(SqlWord.WHERE, "AgtID", SqlWord.Equals, agentId);
				FieldConditions.AddFieldCondition(SqlWord.AND, "QuoteNum", SqlWord.Equals, quoteNum);
				Update();
			}
			catch (GenericSQLException ex)
			{
				bool hayInner = ex.InnerException != null;
				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object> { ColumnsToUpdate, FieldConditions },
					ErrorMessage = "AgentApplicationTransLogService -> update previouse activities InconsequentialActivity to 1 FAILED -> aaTransLogSvc.Update() FAILED -> Error_Info: " + ex.ErrorInfo + " inner message: " + (hayInner ? ex.InnerException.Message : "N/A"),
					ErrorException = ex
				});

			}
			catch (Exception ex)
			{
				bool hayInner = ex.InnerException != null;
				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object> { ColumnsToUpdate, FieldConditions },
					ErrorMessage = "AgentApplicationTransLogService -> update previouse activities InconsequentialActivity to 1 FAILED -> aaTransLogSvc.Update() FAILED -> inner message: " + (hayInner ? ex.InnerException.Message : "N/A"),
					ErrorException = ex
				});

			}
		}
		//Adding the ability to just set one row to Inconsequential.
		public void SetToInconsequentialByActivty(string agentId, string quoteNum, string activity)
		{
			//AgentApplicationTransLogService aaTransLogSvc = null;
			try
			{
				//aaTransLogSvc = new AgentApplicationTransLogService();

				ColumnsToUpdate.AddColumn("InconsequentialActivity", "1");
				FieldConditions.AddFieldCondition(SqlWord.WHERE, "AgtID", SqlWord.Equals, agentId);
				FieldConditions.AddFieldCondition(SqlWord.AND, "QuoteNum", SqlWord.Equals, quoteNum);
				FieldConditions.AddFieldCondition(SqlWord.AND, "Activity", SqlWord.Equals, activity);
				Update();
			}
			catch (GenericSQLException ex)
			{
				bool hayInner = ex.InnerException != null;
				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object> { ColumnsToUpdate, FieldConditions },
					ErrorMessage = "AgentApplicationTransLogService -> update previouse activities InconsequentialActivity to 1 FAILED -> aaTransLogSvc.Update() FAILED -> Error_Info: " + ex.ErrorInfo + " inner message: " + (hayInner ? ex.InnerException.Message : "N/A"),
					ErrorException = ex
				});

			}
			catch (Exception ex)
			{
				bool hayInner = ex.InnerException != null;
				throw _logger.LogErrorWithOptions(new ErrorOptions
				{
					Arguments = new List<object> { ColumnsToUpdate, FieldConditions },
					ErrorMessage = "AgentApplicationTransLogService -> update previouse activities InconsequentialActivity to 1 FAILED -> aaTransLogSvc.Update() FAILED -> inner message: " + (hayInner ? ex.InnerException.Message : "N/A"),
					ErrorException = ex
				});

			}
		}

		public void Add(AgentApplicationTransLog aaTransLog)
		{
			if ((int)(new GenericSQL()).Execute<IAgentApplicationTransLog>(aaTransLog, "[GrandCentral].[dbo].[AgentApplicationTransLog]", GenericSQL.DbAction.insert) < 1)
				throw new Exception("AgentApplicationTransLogService -> INSERT FAILURE: GenericSQL()).Execute<IAgentApplicationTransLog> --> rows returned less than 1 : agtid = " + aaTransLog.AgtID + " quoteNum = " + aaTransLog.QuoteNum);
		}


	}
}



//public override int Update()
//{
//	return (new GenericSQL()).SchemaUpdate(DatabaseAndTableName, base.ColumnsToUpdate, base.FieldConditions, FilterCondtionString, SqlParams);
//}
