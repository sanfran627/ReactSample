using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage.Blob;

namespace SampleReact
{
	public interface IItem
	{
		Guid ItemId { get; }
		string TemplateName();
	}

	public interface IItemWithRef : IItem
	{
		string RefId { get; }
	}

	public abstract class ItemBase : IItem
	{
		[JsonProperty]
		public Guid ItemId { get; protected set; }
		[JsonProperty]
		public DateTime Created { get; protected set; }
		[JsonProperty]
		public DateTime Updated { get; protected set; }

		public string TemplateName()
		{
			string n = this.GetType().Name;
			if( n.EndsWith( "Model" ) )
				return n.Substring( 0, n.Length - "Model".Length );
			else
				return n;
		}
	}
	
	public abstract class ItemBaseWithRef : ItemBase, IItemWithRef
	{
		[JsonProperty]
		public virtual string RefId { get; protected set; }
	}

}
