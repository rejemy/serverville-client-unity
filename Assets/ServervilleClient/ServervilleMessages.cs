﻿
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
	}

	[Serializable]
	public class SetTransientValuesRequest
	{
		public string alias;
		public Dictionary<string,object> values;
	}

	[Serializable]
	public class GetTransientValueRequest
	{
		public string id;
		public string alias;
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
		public string id;
		public string alias;
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
		public string id;
		public string alias;
	}

	[Serializable]
	public class JoinChannelRequest
	{
		public string alias;
		public string id;
		public Dictionary<string,object> values;
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
		public Dictionary<string,object> final_values;
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
		public string alias;
		public string message_type;
		public object value;
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



}