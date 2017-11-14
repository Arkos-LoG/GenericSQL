using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuretyTrust.Common.GenericSQL
{
	public class GenericSQLException : Exception // http://stackoverflow.com/questions/8543264/how-can-i-throw-an-exception-and-add-in-my-own-message-containing-a-key-and-a-va
	{

		public Dictionary<string, string> Errors { get; set; }
		public string ErrorInfo { get; set; }

		public GenericSQLException(string msg, Exception ex)
			: base(msg, ex)
		{
			Errors = new Dictionary<string, string>();
		}

		public GenericSQLException() : this(null, null) { }

		public GenericSQLException(string msg)
			: base(msg)
		{
		}


	}

}
