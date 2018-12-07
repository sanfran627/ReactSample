using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace SampleReact
{
	public enum PasswordMode
	{
		NormalSignin,
		ResetPassword,
		UpdatePassword
	}

	public class UserEntity : EntityBase
	{
		public const string ROWKEY = "user";

		public static string QueryKey(Guid userId) => TableQuery.CombineFilters(
				TableQuery.GenerateFilterCondition(EntityBase.PARTITION_KEY, QueryComparisons.Equal, userId.Encode()),
				TableOperators.And,
				TableQuery.GenerateFilterCondition(EntityBase.ROW_KEY, QueryComparisons.Equal, ROWKEY));


		public static UserEntity CreateFrom(DynamicTableEntity e)
		{
			UserEntity newE = new UserEntity(e.PartitionKey, e.RowKey);
			newE.ETag = e.ETag;
			newE.Timestamp = e.Timestamp;
			newE.ReadEntity(e.Properties, null);
			return newE;
		}

		public override EntityTableType Table => EntityTableType.user;

		public UserEntity() => this.Created = DateTime.UtcNow;

		public UserEntity(string partitionKey, string rowKey) : base(partitionKey, rowKey)
		{
			this.Created = DateTime.UtcNow;
		}

		public UserEntity(Guid userId) : base(userId.Encode(), ROWKEY) => this.Created = DateTime.UtcNow;

		public UserEntity(UserModel user) : base(user.UserId.Encode(), ROWKEY)
		{
			this.SetUpdatedUser(user);
		}

		public void SetUpdatedUser(UserModel user)
		{
			this.User = user;
		}

		public DateTime Created { get; set; }
		public UserModel User { get; set; }

		const string FIELD_CREATED = "created";
		const string FIELD_NAME = "name";
		const string FIELD_TYPE = "type";
		const string FIELD_STATUS = "status";
		const string FIELD_LANGUAGE = "language";
		const string FIELD_PASSWORD = "password";
		const string FIELD_EMAIL = "email";
		const string FIELD_EMAIL_VER = "emailVerification";
		const string FIELD_EMAIL_INV = "emailInvalid";
		const string FIELD_EMAIL_OLD = "emailOld";
		const string FIELD_SITE = "site";

		public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
		{
			this.User = new UserModel(this.PartitionKey.Decode());
			this.Created = DateTime.UtcNow;

			foreach (var kvp in properties)
			{
				switch (kvp.Key)
				{
					case FIELD_CREATED: this.Created = kvp.Value.DateTimeOffsetValue.Value.UtcDateTime; break;
					case FIELD_NAME: this.User.Name = kvp.Value.StringValue ?? string.Empty; break;
					case FIELD_TYPE: this.User.Type = Enum.TryParse<UserType>(kvp.Value.StringValue, out UserType psi) ? psi : UserType.Standard; break;
					case FIELD_STATUS: this.User.StatusId = Enum.TryParse<UserStatusId>(kvp.Value.StringValue, out UserStatusId usi) ? usi : UserStatusId.None; break;
					case FIELD_LANGUAGE: this.User.Language = Enum.TryParse<LanguageId>(kvp.Value.StringValue, out LanguageId li) ? li : LanguageId.None; break;
					case FIELD_PASSWORD: this.User.Password = JsonConvert.DeserializeObject<UserPasswordModel>(kvp.Value.StringValue, JSON.SerializationSettings); break;
					case FIELD_EMAIL: this.User.Email = kvp.Value.StringValue; break;
					case FIELD_EMAIL_VER: this.User.EmailVerification = JsonConvert.DeserializeObject<UserContactVerificationModel>(kvp.Value.StringValue, JSON.SerializationSettings); break;
					case FIELD_EMAIL_INV: this.User.EmailInvalid = kvp.Value.BooleanValue.Value; break;
					case FIELD_EMAIL_OLD: this.User.EmailOld = kvp.Value.StringValue; break;
					case FIELD_SITE: this.User.SiteSettings = JsonConvert.DeserializeObject<UserSiteInfoModel>(kvp.Value.StringValue, JSON.SerializationSettings); break;
					default: break;
				}
			}
		}

		public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
		{
			var items = new Dictionary<string, EntityProperty>();
			items.Add(FIELD_CREATED, new EntityProperty(DateTime.UtcNow));
			items.Add(FIELD_NAME, new EntityProperty(this.User.Name ?? string.Empty));
			items.Add(FIELD_TYPE, new EntityProperty(this.User.Type.ToString()));
			items.Add(FIELD_STATUS, new EntityProperty(this.User.StatusId.ToString()));
			items.Add(FIELD_LANGUAGE, new EntityProperty(this.User.Language.ToString()));
			items.Add(FIELD_PASSWORD, new EntityProperty(JsonConvert.SerializeObject(this.User.Password, JSON.SerializationSettings)));
			items.Add(FIELD_EMAIL, new EntityProperty(this.User.Email ?? string.Empty));
			items.Add(FIELD_EMAIL_INV, new EntityProperty(this.User.EmailInvalid));
			if (!string.IsNullOrWhiteSpace(this.User.EmailOld)) items.Add(FIELD_EMAIL_OLD, new EntityProperty(this.User.EmailOld));
			if (this.User.EmailVerification != null) items.Add(FIELD_EMAIL_VER, new EntityProperty(JsonConvert.SerializeObject(this.User.EmailVerification, JSON.SerializationSettings)));
			if (this.User.SiteSettings != null) items.Add(FIELD_SITE, new EntityProperty(JsonConvert.SerializeObject(this.User.SiteSettings, JSON.SerializationSettings)));
			return items;
		}


		public static DynamicTableEntity UpdateSiteBuilder(Guid userId, UserSiteInfoModel m)
		{
			var entity = new DynamicTableEntity(userId.Encode(), ROWKEY);
			entity.ETag = "*";
			entity.Properties.Add(FIELD_SITE, new EntityProperty(JsonConvert.SerializeObject(m, JSON.SerializationSettings)));
			return entity;
		}

		public static DynamicTableEntity UpdatePasswordBuilder(Guid userId, UserPasswordModel m, PasswordMode mode)
		{
			var entity = new DynamicTableEntity(userId.Encode(), ROWKEY);
			entity.ETag = "*";
			entity.Properties.Add(FIELD_PASSWORD, new EntityProperty(JsonConvert.SerializeObject(m, JSON.SerializationSettings)));
			switch (mode)
			{
				case PasswordMode.NormalSignin:
					//don't touch temp
					break;
				case PasswordMode.ResetPassword: //leaving here as an example only
					entity.Properties.Add("temporary", new EntityProperty(true));
					break;
				case PasswordMode.UpdatePassword: //leaving here as an example only
					entity.Properties.Add("temporary", new EntityProperty(false));
					break;
			}
			return entity;
		}

		public static DynamicTableEntity MarkEmailInvalid(Guid userId)
		{
			var entity = new DynamicTableEntity(userId.Encode(), ROWKEY);
			entity.ETag = "*";
			entity.Properties.Add(FIELD_EMAIL_INV, new EntityProperty(true));
			return entity;
		}

		public static DynamicTableEntity UpdateDisplayName(Guid userId, string name)
		{
			var entity = new DynamicTableEntity(userId.Encode(), ROWKEY);
			entity.ETag = "*";
			entity.Properties.Add(FIELD_NAME, new EntityProperty(name));
			return entity;
		}

		public static DynamicTableEntity UpdateStatus(Guid userId, UserStatusId status)
		{
			var entity = new DynamicTableEntity(userId.Encode(), ROWKEY);
			entity.ETag = "*";
			entity.Properties.Add("status", new EntityProperty(status.ToString()));
			return entity;
		}

		public static DynamicTableEntity UpdateEmailVerification(Guid userId, UserContactVerificationModel model)
		{
			var entity = new DynamicTableEntity(userId.Encode(), ROWKEY);
			entity.ETag = "*";
			entity.Properties.Add(FIELD_EMAIL_VER, new EntityProperty(JsonConvert.SerializeObject(model, JSON.SerializationSettings)));
			return entity;
		}

		public static DynamicTableEntity UpdateEmail(Guid userId, string oldEmail, string newEmail, UserContactVerificationModel model)
		{
			var entity = new DynamicTableEntity(userId.Encode(), ROWKEY);
			entity.ETag = "*";
			entity.Properties.Add(FIELD_EMAIL, new EntityProperty(newEmail));
			entity.Properties.Add(FIELD_EMAIL_OLD, new EntityProperty(oldEmail));
			entity.Properties.Add(FIELD_EMAIL_INV, new EntityProperty(false));
			entity.Properties.Add(FIELD_EMAIL_VER, new EntityProperty(JsonConvert.SerializeObject(model, JSON.SerializationSettings)));
			return entity;
		}
	}
}
