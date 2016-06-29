
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Serverville
{

	[Serializable]
	public class SignIn
	{
		public string username;
		public string email;
		public string password;
	}

	[Serializable]
	public class UserAccountInfo
	{
		public string user_id;
		public string username;
		public string email;
		public string session_id;
	}

	[Serializable]
	public class ValidateSessionRequest
	{
		public string session_id;
	}

	[Serializable]
	public class CreateAnonymousAccount
	{
	}

	[Serializable]
	public class CreateAccount
	{
		public string username;
		public string email;
		public string password;
	}

	[Serializable]
	public class GetUserInfo
	{
	}

	[Serializable]
	public enum JsonDataType
	{
		[EnumMember(Value = "null")]
NULL,
		[EnumMember(Value = "boolean")]
BOOLEAN,
		[EnumMember(Value = "number")]
NUMBER,
		[EnumMember(Value = "string")]
STRING,
		[EnumMember(Value = "json")]
JSON,
		[EnumMember(Value = "xml")]
XML,
		[EnumMember(Value = "datetime")]
DATETIME,
		[EnumMember(Value = "bytes")]
BYTES,
		[EnumMember(Value = "object")]
OBJECT
	}

	[Serializable]
	public class SetUserDataRequest
	{
		public string key;
		public object value;
		public JsonDataType data_type;
	}

	[Serializable]
	public class SetDataReply
	{
		public double updated_at;
	}

	[Serializable]
	public class UserDataRequestList
	{
		public List<SetUserDataRequest> values;
	}

	[Serializable]
	public class KeyRequest
	{
		public string key;
	}

	[Serializable]
	public class DataItemReply
	{
		public string id;
		public string key;
		public object value;
		public JsonDataType data_type;
		public double created;
		public double modified;
		public bool deleted;
	}

	[Serializable]
	public class KeysRequest
	{
		public List<string> keys;
		public double since;
	}

	[Serializable]
	public class UserDataReply
	{
		public Dictionary<string,DataItemReply> values;
	}

	[Serializable]
	public class AllKeysRequest
	{
		public double since;
	}

	[Serializable]
	public class GlobalKeyRequest
	{
		public string id;
		public string key;
	}

	[Serializable]
	public class GlobalKeysRequest
	{
		public string id;
		public List<string> keys;
		public double since;
		public bool include_deleted;
	}

	[Serializable]
	public class AllGlobalKeysRequest
	{
		public string id;
		public double since;
		public bool include_deleted;
	}

	[Serializable]
	public class KeyDataRecordRequest
	{
		public string id;
	}

	[Serializable]
	public class KeyDataInfo
	{
		public string id;
		public string type;
		public string owner;
		public string parent;
		public double version;
		public double created;
		public double modified;
	}

	[Serializable]
	public class SetGlobalDataRequest
	{
		public string id;
		public List<SetUserDataRequest> values;
	}

	[Serializable]
	public class SetTransientValueRequest
	{
		public string alias;
		public string key;
		public object value;
		public JsonDataType data_type;
	}

	[Serializable]
	public class EmptyClientReply
	{
	}

	[Serializable]
	public class SetTransientValueItem
	{
		public string key;
		public object value;
		public JsonDataType data_type;
	}

	[Serializable]
	public class SetTransientValuesRequest
	{
		public string alias;
		public List<SetTransientValueItem> values;
	}

	[Serializable]
	public class GetTransientValueRequest
	{
		public string id;
		public string alias;
		public string key;
	}

	[Serializable]
	public class GetTransientValuesRequest
	{
		public string id;
		public string alias;
		public List<string> keys;
	}

	[Serializable]
	public class GetAllTransientValuesRequest
	{
		public string id;
		public string alias;
	}

	[Serializable]
	public class JoinChannelRequest
	{
		public string alias;
		public string id;
	}

	[Serializable]
	public class ChannelMemberInfo
	{
		public string id;
		public Dictionary<string,object> values;
	}

	[Serializable]
	public class ChannelInfo
	{
		public string id;
		public Dictionary<string,object> values;
		public Dictionary<string,ChannelMemberInfo> members;
	}

	[Serializable]
	public class LeaveChannelRequest
	{
		public string alias;
		public string id;
	}

	[Serializable]
	public class ListenToResidentRequest
	{
		public string id;
	}

	[Serializable]
	public class StopListenToResidentRequest
	{
		public string id;
	}

	[Serializable]
	public class TransientMessageRequest
	{
		public string to;
		public string message_type;
		public object value;
		public JsonDataType data_type;
	}



}