using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Serverville
{
	class ServervilleClientComponent : MonoBehaviour 
	{
		private ServervilleClient Client;

		public static ServervilleClientComponent Get(ServervilleClient client)
		{
			GameObject obj = GameObject.Find("/Serverville");
			if(obj == null)
			{
				obj = new GameObject("Serverville");
				DontDestroyOnLoad(obj);
			}

			ServervilleClientComponent comp = obj.GetComponent<ServervilleClientComponent>();
			if(comp == null)
			{
				comp = obj.AddComponent<ServervilleClientComponent>();
				comp.Client = client;
			}

			return comp;
		}

		public void StartPingTimer(float period)
		{
			InvokeRepeating("PingTimer", period, period);
		}

		public void StopPingTimer()
		{
			CancelInvoke("PingTimer");
		}

		public void PingTimer()
		{
			Client.Ping();
		}
	}

	public class ServervilleClient
	{
		public delegate void OnInitComplete(SignInReply userInfo, ErrorReply err);

		internal string ServerURL;
		internal string SessionId;

		private UserAccountInfo UserInfo;

		public bool LogMessages = false;

		private ServervilleTransport Transport;

		public delegate void ErrorHandlerDelegate(ErrorReply err);
		public delegate void ServerMessageTypeHandlerDelegate(string from, string json);
		public delegate void ServerMessageHandlerDelegate(string messageType, string from, string json);

		public ErrorHandlerDelegate GlobalErrorHandler;
		public ServerMessageHandlerDelegate ServerMessageHandler;
		public Dictionary<string,ServerMessageTypeHandlerDelegate> ServerMessageTypeHandlers;

		public float PingPeriod = 5.0f;
		private float LastSend = 0.0f;

		private long LastServerTime = 0;
		private long LastServerTimeAt = 0;

		public ServervilleClient(string url)
		{
			ServerURL = url;
			SessionId = PlayerPrefs.GetString("Serverville"+ServerURL+"SessionId", null);
			if(SessionId != null && SessionId.Length == 0)
				SessionId = null;

			ServerMessageTypeHandlers = new Dictionary<string,ServerMessageTypeHandlerDelegate>();

			if(ServerURL.StartsWith("http://") || ServerURL.StartsWith("https://"))
			{
				Transport = new ServervilleHttp(this);
			}
			else if(ServerURL.StartsWith("ws://") || ServerURL.StartsWith("wss://"))
			{
				Transport = new ServervilleWS(this);
			}
			else
			{
				throw new Exception("Unknown server protocol: "+url);
			}
		}

		public void Init(OnInitComplete onComplete)
		{
			Transport.Init(delegate(ErrorReply initErr)
				{
					if(initErr != null)
					{
						onComplete(null, initErr);
						return;
					}

					if(SessionId != null)
					{
						ValidateSession(SessionId, delegate(SignInReply reply)
							{
								onComplete(reply, null);
							},
							delegate(ErrorReply err)
							{
								SignOut();
								onComplete(null, err);
							});
					}
					else
					{
						onComplete(null, null);
					}

				});
		}

		private void SetUserInfo(SignInReply reply)
		{
			if(reply == null)
			{
				UserInfo = null;
				SessionId = null;
				PlayerPrefs.DeleteKey("Serverville"+ServerURL+"SessionId");
			}
			else
			{
				UserInfo = new UserAccountInfo();
				UserInfo.email = reply.email;
				UserInfo.session_id = reply.session_id;
				UserInfo.username = reply.username;
				UserInfo.user_id = reply.user_id;
				SessionId = reply.session_id;
				PlayerPrefs.SetString("Serverville"+ServerURL+"SessionId", SessionId);
				PlayerPrefs.Save();

				setServerTime((long)reply.time);
			}
		}

		public bool IsSignedIn()
		{
			return SessionId != null;
		}

		public void SignOut()
		{
			SetUserInfo(null);
		}

		public UserAccountInfo GetUserInfo()
		{
			return UserInfo;
		}

		internal void OnServerError(ErrorReply err)
		{
			if(GlobalErrorHandler != null)
				GlobalErrorHandler(err);
		
			if(err.errorCode == 19) // Session expired
			{
				Shutdown();
			}
		}

		internal void OnServerMessage(string messageId, string from, string via, string jsonData)
		{
			if(messageId == "_error")
			{
				// Pushed error
				ErrorReply err = JsonConvert.DeserializeObject<ErrorReply>(jsonData, ServervilleHttp.JsonSettings);
				OnServerError(err);
				return;
			}

			ServerMessageTypeHandlerDelegate handler = null;
			ServerMessageTypeHandlers.TryGetValue(messageId, out handler);
			if(handler != null)
			{
				handler(from, jsonData);
			}
			else if(ServerMessageHandler != null)
			{
				ServerMessageHandler(messageId, from, jsonData);
			}
			else
			{
				Debug.Log("No handler for message type: "+messageId);
			}
		}

		internal void OnTransportClosed()
		{
			StopPingTimer();
			OnServerError(ErrorReply.makeClientErrorCode(-1, "Closed"));
		}

		internal void StartPingTimer()
		{
			ServervilleClientComponent comp = ServervilleClientComponent.Get(this);
			comp.StartPingTimer(PingPeriod);
		}

		internal void StopPingTimer()
		{
			ServervilleClientComponent comp = ServervilleClientComponent.Get(this);
			comp.StopPingTimer();
		}

		internal void Ping()
		{
			if(Time.unscaledTime - LastSend < 4.0)
				return;

			GetTime(delegate(ServerTime reply)
				{
					setServerTime((long)reply.time);
				},
				null);
		}

		internal void setServerTime(long time)
		{
			LastServerTime = time;
			LastServerTimeAt = (long)(Time.unscaledTime * 1000.0);
		}

		long getServerTime()
		{
			if(LastServerTime == 0)
				return 0;
			return ((long)(Time.unscaledTime * 1000.0) - LastServerTimeAt) + LastServerTime;
		}

		float getLastSendTime()
		{
			return LastSend;
		}

		public void Shutdown()
		{
			if(Transport != null)
				Transport.Close();
		}

        public void ApiByName<ReqType,ReplyType>(string api, ReqType request, Action<ReplyType> onSuccess, OnErrorReply onErr)
		{
			Transport.CallAPI<ReplyType>(api, request,
				onSuccess,
				onErr
            ); 

            LastSend = Time.unscaledTime;
		}
        
		public void SignIn(SignIn request, Action<SignInReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<SignIn,SignInReply>("SignIn", request,
				delegate(SignInReply reply) { SetUserInfo(reply); if(onSuccess != null) { onSuccess(reply); } },
				onErr
            ); 
		}

		public void SignIn(string username, string email, string password, Action<SignInReply> onSuccess, OnErrorReply onErr)
		{
			SignIn(
            new SignIn
				{
					username = username,
					email = email,
					password = password
				},
				onSuccess,
                onErr
           ); 
		}
		public void ValidateSession(ValidateSessionRequest request, Action<SignInReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<ValidateSessionRequest,SignInReply>("ValidateSession", request,
				delegate(SignInReply reply) { SetUserInfo(reply); if(onSuccess != null) { onSuccess(reply); } },
				onErr
            ); 
		}

		public void ValidateSession(string session_id, Action<SignInReply> onSuccess, OnErrorReply onErr)
		{
			ValidateSession(
            new ValidateSessionRequest
				{
					session_id = session_id
				},
				onSuccess,
                onErr
           ); 
		}
		public void CreateAnonymousAccount(CreateAnonymousAccount request, Action<SignInReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<CreateAnonymousAccount,SignInReply>("CreateAnonymousAccount", request,
				delegate(SignInReply reply) { SetUserInfo(reply); if(onSuccess != null) { onSuccess(reply); } },
				onErr
            ); 
		}

		public void CreateAnonymousAccount(string invite_code, Action<SignInReply> onSuccess, OnErrorReply onErr)
		{
			CreateAnonymousAccount(
            new CreateAnonymousAccount
				{
					invite_code = invite_code
				},
				onSuccess,
                onErr
           ); 
		}
		public void CreateAccount(CreateAccount request, Action<SignInReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<CreateAccount,SignInReply>("CreateAccount", request,
				delegate(SignInReply reply) { SetUserInfo(reply); if(onSuccess != null) { onSuccess(reply); } },
				onErr
            ); 
		}

		public void CreateAccount(string username, string email, string password, string invite_code, Action<SignInReply> onSuccess, OnErrorReply onErr)
		{
			CreateAccount(
            new CreateAccount
				{
					username = username,
					email = email,
					password = password,
					invite_code = invite_code
				},
				onSuccess,
                onErr
           ); 
		}
		public void ConvertToFullAccount(CreateAccount request, Action<SignInReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<CreateAccount,SignInReply>("ConvertToFullAccount", request,
				delegate(SignInReply reply) { SetUserInfo(reply); if(onSuccess != null) { onSuccess(reply); } },
				onErr
            ); 
		}

		public void ConvertToFullAccount(string username, string email, string password, string invite_code, Action<SignInReply> onSuccess, OnErrorReply onErr)
		{
			ConvertToFullAccount(
            new CreateAccount
				{
					username = username,
					email = email,
					password = password,
					invite_code = invite_code
				},
				onSuccess,
                onErr
           ); 
		}
		public void GetTime(EmptyClientRequest request, Action<ServerTime> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<EmptyClientRequest,ServerTime>("GetTime", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetTime(Action<ServerTime> onSuccess, OnErrorReply onErr)
		{
			GetTime(
            new EmptyClientRequest
				{

				},
				onSuccess,
                onErr
           ); 
		}
		public void GetUserInfo(GetUserInfo request, Action<UserAccountInfo> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<GetUserInfo,UserAccountInfo>("GetUserInfo", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetUserInfo(Action<UserAccountInfo> onSuccess, OnErrorReply onErr)
		{
			GetUserInfo(
            new GetUserInfo
				{

				},
				onSuccess,
                onErr
           ); 
		}
		public void SetUserKey(SetUserDataRequest request, Action<SetDataReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<SetUserDataRequest,SetDataReply>("SetUserKey", request,
				onSuccess,
				onErr
            ); 
		}

		public void SetUserKey(string key, object value, JsonDataType data_type, Action<SetDataReply> onSuccess, OnErrorReply onErr)
		{
			SetUserKey(
            new SetUserDataRequest
				{
					key = key,
					value = value,
					data_type = data_type
				},
				onSuccess,
                onErr
           ); 
		}
		public void SetUserKeys(UserDataRequestList request, Action<SetDataReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<UserDataRequestList,SetDataReply>("SetUserKeys", request,
				onSuccess,
				onErr
            ); 
		}

		public void SetUserKeys(List<SetUserDataRequest> values, Action<SetDataReply> onSuccess, OnErrorReply onErr)
		{
			SetUserKeys(
            new UserDataRequestList
				{
					values = values
				},
				onSuccess,
                onErr
           ); 
		}
		public void GetUserKey(KeyRequest request, Action<DataItemReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<KeyRequest,DataItemReply>("GetUserKey", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetUserKey(string key, Action<DataItemReply> onSuccess, OnErrorReply onErr)
		{
			GetUserKey(
            new KeyRequest
				{
					key = key
				},
				onSuccess,
                onErr
           ); 
		}
		public void GetUserKeys(KeysRequest request, Action<UserDataReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<KeysRequest,UserDataReply>("GetUserKeys", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetUserKeys(List<string> keys, double since, Action<UserDataReply> onSuccess, OnErrorReply onErr)
		{
			GetUserKeys(
            new KeysRequest
				{
					keys = keys,
					since = since
				},
				onSuccess,
                onErr
           ); 
		}
		public void GetAllUserKeys(AllKeysRequest request, Action<UserDataReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<AllKeysRequest,UserDataReply>("GetAllUserKeys", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetAllUserKeys(double since, Action<UserDataReply> onSuccess, OnErrorReply onErr)
		{
			GetAllUserKeys(
            new AllKeysRequest
				{
					since = since
				},
				onSuccess,
                onErr
           ); 
		}
		public void GetDataKey(GlobalKeyRequest request, Action<DataItemReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<GlobalKeyRequest,DataItemReply>("GetDataKey", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetDataKey(string id, string key, Action<DataItemReply> onSuccess, OnErrorReply onErr)
		{
			GetDataKey(
            new GlobalKeyRequest
				{
					id = id,
					key = key
				},
				onSuccess,
                onErr
           ); 
		}
		public void GetDataKeys(GlobalKeysRequest request, Action<UserDataReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<GlobalKeysRequest,UserDataReply>("GetDataKeys", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetDataKeys(string id, List<string> keys, double since, bool include_deleted, Action<UserDataReply> onSuccess, OnErrorReply onErr)
		{
			GetDataKeys(
            new GlobalKeysRequest
				{
					id = id,
					keys = keys,
					since = since,
					include_deleted = include_deleted
				},
				onSuccess,
                onErr
           ); 
		}
		public void GetAllDataKeys(AllGlobalKeysRequest request, Action<UserDataReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<AllGlobalKeysRequest,UserDataReply>("GetAllDataKeys", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetAllDataKeys(string id, double since, bool include_deleted, Action<UserDataReply> onSuccess, OnErrorReply onErr)
		{
			GetAllDataKeys(
            new AllGlobalKeysRequest
				{
					id = id,
					since = since,
					include_deleted = include_deleted
				},
				onSuccess,
                onErr
           ); 
		}
		public void GetKeyDataRecord(KeyDataRecordRequest request, Action<KeyDataInfo> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<KeyDataRecordRequest,KeyDataInfo>("GetKeyDataRecord", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetKeyDataRecord(string id, Action<KeyDataInfo> onSuccess, OnErrorReply onErr)
		{
			GetKeyDataRecord(
            new KeyDataRecordRequest
				{
					id = id
				},
				onSuccess,
                onErr
           ); 
		}
		public void SetDataKeys(SetGlobalDataRequest request, Action<SetDataReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<SetGlobalDataRequest,SetDataReply>("SetDataKeys", request,
				onSuccess,
				onErr
            ); 
		}

		public void SetDataKeys(string id, List<SetUserDataRequest> values, Action<SetDataReply> onSuccess, OnErrorReply onErr)
		{
			SetDataKeys(
            new SetGlobalDataRequest
				{
					id = id,
					values = values
				},
				onSuccess,
                onErr
           ); 
		}
		public void SetTransientValue(SetTransientValueRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<SetTransientValueRequest,EmptyClientReply>("SetTransientValue", request,
				onSuccess,
				onErr
            ); 
		}

		public void SetTransientValue(string alias, string key, object value, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			SetTransientValue(
            new SetTransientValueRequest
				{
					alias = alias,
					key = key,
					value = value
				},
				onSuccess,
                onErr
           ); 
		}
		public void SetTransientValues(SetTransientValuesRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<SetTransientValuesRequest,EmptyClientReply>("SetTransientValues", request,
				onSuccess,
				onErr
            ); 
		}

		public void SetTransientValues(string alias, Dictionary<string,object> values, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			SetTransientValues(
            new SetTransientValuesRequest
				{
					alias = alias,
					values = values
				},
				onSuccess,
                onErr
           ); 
		}
		public void GetTransientValue(GetTransientValueRequest request, Action<TransientDataItemReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<GetTransientValueRequest,TransientDataItemReply>("GetTransientValue", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetTransientValue(string id, string alias, string key, Action<TransientDataItemReply> onSuccess, OnErrorReply onErr)
		{
			GetTransientValue(
            new GetTransientValueRequest
				{
					id = id,
					alias = alias,
					key = key
				},
				onSuccess,
                onErr
           ); 
		}
		public void GetTransientValues(GetTransientValuesRequest request, Action<TransientDataItemsReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<GetTransientValuesRequest,TransientDataItemsReply>("GetTransientValues", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetTransientValues(string id, string alias, List<string> keys, Action<TransientDataItemsReply> onSuccess, OnErrorReply onErr)
		{
			GetTransientValues(
            new GetTransientValuesRequest
				{
					id = id,
					alias = alias,
					keys = keys
				},
				onSuccess,
                onErr
           ); 
		}
		public void GetAllTransientValues(GetAllTransientValuesRequest request, Action<TransientDataItemsReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<GetAllTransientValuesRequest,TransientDataItemsReply>("GetAllTransientValues", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetAllTransientValues(string id, string alias, Action<TransientDataItemsReply> onSuccess, OnErrorReply onErr)
		{
			GetAllTransientValues(
            new GetAllTransientValuesRequest
				{
					id = id,
					alias = alias
				},
				onSuccess,
                onErr
           ); 
		}
		public void JoinChannel(JoinChannelRequest request, Action<ChannelInfo> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<JoinChannelRequest,ChannelInfo>("JoinChannel", request,
				onSuccess,
				onErr
            ); 
		}

		public void JoinChannel(string alias, string id, Dictionary<string,object> values, Action<ChannelInfo> onSuccess, OnErrorReply onErr)
		{
			JoinChannel(
            new JoinChannelRequest
				{
					alias = alias,
					id = id,
					values = values
				},
				onSuccess,
                onErr
           ); 
		}
		public void LeaveChannel(LeaveChannelRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<LeaveChannelRequest,EmptyClientReply>("LeaveChannel", request,
				onSuccess,
				onErr
            ); 
		}

		public void LeaveChannel(string alias, string id, Dictionary<string,object> final_values, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			LeaveChannel(
            new LeaveChannelRequest
				{
					alias = alias,
					id = id,
					final_values = final_values
				},
				onSuccess,
                onErr
           ); 
		}
		public void AddAliasToChannel(JoinChannelRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<JoinChannelRequest,EmptyClientReply>("AddAliasToChannel", request,
				onSuccess,
				onErr
            ); 
		}

		public void AddAliasToChannel(string alias, string id, Dictionary<string,object> values, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			AddAliasToChannel(
            new JoinChannelRequest
				{
					alias = alias,
					id = id,
					values = values
				},
				onSuccess,
                onErr
           ); 
		}
		public void RemoveAliasFromChannel(LeaveChannelRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<LeaveChannelRequest,EmptyClientReply>("RemoveAliasFromChannel", request,
				onSuccess,
				onErr
            ); 
		}

		public void RemoveAliasFromChannel(string alias, string id, Dictionary<string,object> final_values, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			RemoveAliasFromChannel(
            new LeaveChannelRequest
				{
					alias = alias,
					id = id,
					final_values = final_values
				},
				onSuccess,
                onErr
           ); 
		}
		public void ListenToChannel(ListenToResidentRequest request, Action<ChannelInfo> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<ListenToResidentRequest,ChannelInfo>("ListenToChannel", request,
				onSuccess,
				onErr
            ); 
		}

		public void ListenToChannel(string id, Action<ChannelInfo> onSuccess, OnErrorReply onErr)
		{
			ListenToChannel(
            new ListenToResidentRequest
				{
					id = id
				},
				onSuccess,
                onErr
           ); 
		}
		public void StopListenToChannel(StopListenToResidentRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<StopListenToResidentRequest,EmptyClientReply>("StopListenToChannel", request,
				onSuccess,
				onErr
            ); 
		}

		public void StopListenToChannel(string id, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			StopListenToChannel(
            new StopListenToResidentRequest
				{
					id = id
				},
				onSuccess,
                onErr
           ); 
		}
		public void SendClientMessage(TransientMessageRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<TransientMessageRequest,EmptyClientReply>("SendClientMessage", request,
				onSuccess,
				onErr
            ); 
		}

		public void SendClientMessage(string to, string alias, string message_type, object value, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			SendClientMessage(
            new TransientMessageRequest
				{
					to = to,
					alias = alias,
					message_type = message_type,
					value = value
				},
				onSuccess,
                onErr
           ); 
		}
		public void GetCurrencyBalance(CurrencyBalanceRequest request, Action<CurrencyBalanceReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<CurrencyBalanceRequest,CurrencyBalanceReply>("GetCurrencyBalance", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetCurrencyBalance(string currency_id, Action<CurrencyBalanceReply> onSuccess, OnErrorReply onErr)
		{
			GetCurrencyBalance(
            new CurrencyBalanceRequest
				{
					currency_id = currency_id
				},
				onSuccess,
                onErr
           ); 
		}
		public void GetCurrencyBalances(EmptyClientRequest request, Action<CurrencyBalancesReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<EmptyClientRequest,CurrencyBalancesReply>("GetCurrencyBalances", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetCurrencyBalances(Action<CurrencyBalancesReply> onSuccess, OnErrorReply onErr)
		{
			GetCurrencyBalances(
            new EmptyClientRequest
				{

				},
				onSuccess,
                onErr
           ); 
		}


	}

}