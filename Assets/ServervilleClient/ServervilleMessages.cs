
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Serverville
{

	[Serializable]
	public class SetLocaleRequest
	{
		public string country;
		public string language;
	}

	[Serializable]
	public class EmptyClientReply
	{
	}

	[Serializable]
	public class SignIn
	{
		public string username;
		public string email;
		public string password;
	}

	[Serializable]
	public class SignInReply
	{
		public string user_id;
		public string username;
		public string email;
		public string session_id;
		public double admin_level;
		public string language;
		public string country;
		public double time;
	}

	[Serializable]
	public class ValidateSessionRequest
	{
		public string session_id;
	}

	[Serializable]
	public class CreateAnonymousAccount
	{
		public string invite_code;
		public string language;
		public string country;
	}

	[Serializable]
	public class CreateAccount
	{
		public string username;
		public string email;
		public string password;
		public string invite_code;
		public string language;
		public string country;
	}

	[Serializable]
	public class ChangePasswordRequest
	{
		public string old_password;
		public string new_password;
	}

	[Serializable]
	public class ChangePasswordReply
	{
		public string session_id;
	}

	[Serializable]
	public class EmptyClientRequest
	{
	}

	[Serializable]
	public class ServerTime
	{
		public double time;
	}

	[Serializable]
	public class GetUserInfo
	{
	}

	[Serializable]
	public class UserAccountInfo
	{
		public string user_id;
		public string username;
		public string email;
		public string session_id;
		public double admin_level;
	}

	[Serializable]
	public class GetUserDataComboRequest
	{
		public double since;
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
	public class GetUserDataComboReply
	{
		public Dictionary<string,DataItemReply> values;
		public Dictionary<string,int> balances;
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
	public class UserDataSetAndDeleteRequestList
	{
		public List<SetUserDataRequest> values;
		public List<string> delete_keys;
	}

	[Serializable]
	public class KeyRequest
	{
		public string key;
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
	public class DeleteKeyRequest
	{
		public string key;
	}

	[Serializable]
	public class DeleteKeysRequest
	{
		public List<string> keys;
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
	public class PageGlobalKeysRequest
	{
		public string id;
		public double page_size;
		public string start_after;
		public bool descending;
	}

	[Serializable]
	public class OrderedDataReply
	{
		public List<DataItemReply> values;
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
		public string record_type;
		public string owner;
		public string parent;
		public double version;
		public double created;
		public double modified;
	}

	[Serializable]
	public class KeyDataRecordsRequest
	{
		public string record_type;
		public string parent;
	}

	[Serializable]
	public class KeyDataRecords
	{
		public List<KeyDataInfo> records;
	}

	[Serializable]
	public class SetGlobalDataRequest
	{
		public string id;
		public List<SetUserDataRequest> values;
	}

	[Serializable]
	public class GetHostWithResidentRequest
	{
		public string resident_id;
	}

	[Serializable]
	public class GetHostWithResidentReply
	{
		public string host;
	}

	[Serializable]
	public class CreateResidentRequest
	{
		public string resident_type;
		public Dictionary<string,object> values;
	}

	[Serializable]
	public class CreateResidentReply
	{
		public string resident_id;
	}

	[Serializable]
	public class DeleteResidentRequest
	{
		public string resident_id;
		public Dictionary<string,object> final_values;
	}

	[Serializable]
	public class RemoveResidentFromAllChannelsRequest
	{
		public string resident_id;
		public Dictionary<string,object> final_values;
	}

	[Serializable]
	public class SetTransientValueRequest
	{
		public string resident_id;
		public string key;
		public object value;
	}

	[Serializable]
	public class SetTransientValuesRequest
	{
		public string resident_id;
		public Dictionary<string,object> values;
	}

	[Serializable]
	public class DeleteTransientValueRequest
	{
		public string resident_id;
		public string key;
	}

	[Serializable]
	public class DeleteTransientValuesRequest
	{
		public string resident_id;
		public List<string> values;
	}

	[Serializable]
	public class GetTransientValueRequest
	{
		public string resident_id;
		public string key;
	}

	[Serializable]
	public class TransientDataItemReply
	{
		public object value;
	}

	[Serializable]
	public class GetTransientValuesRequest
	{
		public string resident_id;
		public List<string> keys;
	}

	[Serializable]
	public class TransientDataItemsReply
	{
		public Dictionary<string,object> values;
	}

	[Serializable]
	public class GetAllTransientValuesRequest
	{
		public string resident_id;
	}

	[Serializable]
	public class JoinChannelRequest
	{
		public string channel_id;
		public string resident_id;
		public Dictionary<string,object> values;
	}

	[Serializable]
	public class ChannelMemberInfo
	{
		public string resident_id;
		public Dictionary<string,object> values;
	}

	[Serializable]
	public class ChannelInfo
	{
		public string channel_id;
		public Dictionary<string,object> values;
		public Dictionary<string,ChannelMemberInfo> members;
	}

	[Serializable]
	public class LeaveChannelRequest
	{
		public string channel_id;
		public string resident_id;
		public Dictionary<string,object> final_values;
	}

	[Serializable]
	public class ListenToChannelRequest
	{
		public string channel_id;
	}

	[Serializable]
	public class StopListenToChannelRequest
	{
		public string channel_id;
	}

	[Serializable]
	public class TriggerResidentEventRequest
	{
		public string resident_id;
		public string event_type;
		public string event_data;
	}

	[Serializable]
	public class SendUserMessageRequest
	{
		public string to;
		public string message_type;
		public string message;
		public bool guaranteed;
	}

	[Serializable]
	public class UserMessageNotification
	{
		public string id;
		public string message_type;
		public string message;
		public string from_id;
		public bool sender_is_user;
	}

	[Serializable]
	public class UserMessageList
	{
		public List<UserMessageNotification> messages;
	}

	[Serializable]
	public class ClearMessageRequest
	{
		public string id;
	}

	[Serializable]
	public class CurrencyBalanceRequest
	{
		public string currency_id;
	}

	[Serializable]
	public class CurrencyBalanceReply
	{
		public string currency_id;
		public int balance;
	}

	[Serializable]
	public class CurrencyBalancesReply
	{
		public Dictionary<string,int> balances;
	}

	[Serializable]
	public class GetProductsRequest
	{
	}

	[Serializable]
	public class ProductInfo
	{
		public string id;
		public string name;
		public string description;
		public string image_url;
		public double price;
		public string display_price;
		public string currency;
	}

	[Serializable]
	public class ProductInfoList
	{
		public List<ProductInfo> products;
	}

	[Serializable]
	public class GetProductRequest
	{
		public string product_id;
	}

	[Serializable]
	public class StripeCheckoutRequest
	{
		public string stripe_token;
		public string product_id;
	}

	[Serializable]
	public class ProductPurchasedReply
	{
		public string product_id;
		public double price;
		public Dictionary<string,int> currencies;
	}

	[Serializable]
	public class ResidentJoinedNotification
	{
		public string resident_id;
		public string via_channel;
		public Dictionary<string,object> values;
	}

	[Serializable]
	public class ResidentStateUpdateNotification
	{
		public string resident_id;
		public string via_channel;
		public Dictionary<string,object> values;
		public List<string> deleted;
	}

	[Serializable]
	public class ResidentLeftNotification
	{
		public string resident_id;
		public string via_channel;
		public Dictionary<string,object> final_values;
	}

	[Serializable]
	public class ResidentEventNotification
	{
		public string resident_id;
		public string via_channel;
		public string event_type;
		public string event_data;
	}

	[Serializable]
	public class PendingNotification
	{
		public string notification_type;
		public string body;
	}

	[Serializable]
	public class PendingNotificationList
	{
		public List<PendingNotification> notifications;
	}



}