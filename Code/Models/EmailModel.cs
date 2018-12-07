using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SampleReact
{
	public class EmailComposite
	{
		public EmailModel Email { get; set; }
		public EmailEntity EmailEntity { get; set; }

		public bool HasUser => this.Email.UserId != null && this.Email.UserId != Guid.Empty;
	}

	public class EmailModel
	{
		public EmailModel() { }
		public EmailModel( string email )
		{
			this.Email = email;
			this.UserId = Guid.Empty;
		}

		public EmailModel( string email, Guid userId )
		{
			this.Email = email;
			this.UserId = userId;
		}

		public string Email { get; set; }
		public Guid UserId { get; set; }
	}
}
