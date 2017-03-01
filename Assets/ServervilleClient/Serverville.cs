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
		public delegate void OnSwitchHostsComplete(ErrorReply err);

		internal string ServerURL;
		internal string ServerHost;
		internal string ServerProtocol;

		internal string SessionId;

		private UserAccountInfo UserInfo;

		public bool LogMessages = false;

		private ServervilleTransport Transport;

		public delegate void ErrorHandlerDelegate(ErrorReply err);
		public delegate void UserMessageHandlerDelegate(UserMessageNotification message);

		public ErrorHandlerDelegate GlobalErrorHandler;
		public UserMessageHandlerDelegate ServerMessageHandler;
		public Dictionary<string,UserMessageHandlerDelegate> ServerMessageTypeHandlers;

		public float PingPeriod = 5.0f;
		private float LastSend = 0.0f;

		private long LastServerTime = 0;
		private long LastServerTimeAt = 0;

		public ServervilleClient(string url)
		{
			SessionId = PlayerPrefs.GetString("Serverville"+ServerURL+"SessionId", null);
			if(SessionId != null && SessionId.Length == 0)
				SessionId = null;
			
			InitServerURL(url);

			ServerMessageTypeHandlers = new Dictionary<string,UserMessageHandlerDelegate>();
		}

		private void InitServerURL(string url)
		{
			ServerURL = url;
			int protocolLength = url.IndexOf("://");
			if(protocolLength < 2)
				throw new Exception("Malformed url: "+url);
			ServerHost = ServerURL.Substring(protocolLength+3);
			ServerProtocol = ServerURL.Substring(0, protocolLength);

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
						ValidateSession(SessionId,
							delegate(SignInReply reply)
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

				}
			);
		}

		public void SwitchHosts(string host, OnSwitchHostsComplete onComplete)
		{
			string url = host;
			if(host.IndexOf("://") < 0)
			{
				url = ServerProtocol + "://"+host;
			}

			if(ServerURL == host)
			{
				onComplete(null);
				return;
			}

			Shutdown();

			InitServerURL(url);
			Init(delegate(SignInReply userInfo, ErrorReply err) {
				onComplete(err);
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

		internal void OnServerNotification(string notificationType, string notificationJson)
		{
			switch(notificationType)
			{
				case "error":
				{
					// Pushed error
					ErrorReply err = JsonConvert.DeserializeObject<ErrorReply>(notificationJson, ServervilleHttp.JsonSettings);
					OnServerError(err);
					return;
				}
				case "msg":
				{
					UserMessageNotification note = JsonConvert.DeserializeObject<UserMessageNotification>(notificationJson, ServervilleHttp.JsonSettings);
					OnUserMessage(note);
					return;
				}
				case "resJoined":
				{
					
					return;
				}
				case "resLeft":
				{

					return;
				}
				case "resEvent":
				{

					return;
				}
				case "resUpdate":
				{

					return;
				}
				default:
				{
					Debug.Log("Unknown server notification type: "+notificationType);
					return;
				}
			}

		}

		internal void OnUserMessage(UserMessageNotification message)
		{
			UserMessageHandlerDelegate handler = null;
			ServerMessageTypeHandlers.TryGetValue(message.message_type, out handler);
			if(handler != null)
			{
				handler(message);
			}
			else if(ServerMessageHandler != null)
			{
				ServerMessageHandler(message);
			}
			else
			{
				Debug.Log("No handler for use message type: "+message.message_type);
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

		public void CreateAnonymousAccount(string invite_code, string language, string country, Action<SignInReply> onSuccess, OnErrorReply onErr)
		{
			CreateAnonymousAccount(
            new CreateAnonymousAccount
				{
					invite_code = invite_code,
					language = language,
					country = country
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

		public void CreateAccount(string username, string email, string password, string invite_code, string language, string country, Action<SignInReply> onSuccess, OnErrorReply onErr)
		{
			CreateAccount(
            new CreateAccount
				{
					username = username,
					email = email,
					password = password,
					invite_code = invite_code,
					language = language,
					country = country
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

		public void ConvertToFullAccount(string username, string email, string password, string invite_code, string language, string country, Action<SignInReply> onSuccess, OnErrorReply onErr)
		{
			ConvertToFullAccount(
            new CreateAccount
				{
					username = username,
					email = email,
					password = password,
					invite_code = invite_code,
					language = language,
					country = country
				},
				onSuccess,
                onErr
           ); 
		}
		public void ChangePassword(ChangePasswordRequest request, Action<ChangePasswordReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<ChangePasswordRequest,ChangePasswordReply>("ChangePassword", request,
				onSuccess,
				onErr
            ); 
		}

		public void ChangePassword(string old_password, string new_password, Action<ChangePasswordReply> onSuccess, OnErrorReply onErr)
		{
			ChangePassword(
            new ChangePasswordRequest
				{
					old_password = old_password,
					new_password = new_password
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
		public void SetLocale(SetLocaleRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<SetLocaleRequest,EmptyClientReply>("SetLocale", request,
				onSuccess,
				onErr
            ); 
		}

		public void SetLocale(string country, string language, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			SetLocale(
            new SetLocaleRequest
				{
					country = country,
					language = language
				},
				onSuccess,
                onErr
           ); 
		}
		public void GetUserDataCombo(GetUserDataComboRequest request, Action<GetUserDataComboReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<GetUserDataComboRequest,GetUserDataComboReply>("GetUserDataCombo", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetUserDataCombo(double since, Action<GetUserDataComboReply> onSuccess, OnErrorReply onErr)
		{
			GetUserDataCombo(
            new GetUserDataComboRequest
				{
					since = since
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
		public void DeleteUserKey(DeleteKeyRequest request, Action<SetDataReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<DeleteKeyRequest,SetDataReply>("DeleteUserKey", request,
				onSuccess,
				onErr
            ); 
		}

		public void DeleteUserKey(string key, Action<SetDataReply> onSuccess, OnErrorReply onErr)
		{
			DeleteUserKey(
            new DeleteKeyRequest
				{
					key = key
				},
				onSuccess,
                onErr
           ); 
		}
		public void DeleteUserKeys(DeleteKeysRequest request, Action<SetDataReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<DeleteKeysRequest,SetDataReply>("DeleteUserKeys", request,
				onSuccess,
				onErr
            ); 
		}

		public void DeleteUserKeys(List<string> keys, Action<SetDataReply> onSuccess, OnErrorReply onErr)
		{
			DeleteUserKeys(
            new DeleteKeysRequest
				{
					keys = keys
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
		public void PageAllDataKeys(PageGlobalKeysRequest request, Action<OrderedDataReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<PageGlobalKeysRequest,OrderedDataReply>("PageAllDataKeys", request,
				onSuccess,
				onErr
            ); 
		}

		public void PageAllDataKeys(string id, double page_size, string start_after, bool descending, Action<OrderedDataReply> onSuccess, OnErrorReply onErr)
		{
			PageAllDataKeys(
            new PageGlobalKeysRequest
				{
					id = id,
					page_size = page_size,
					start_after = start_after,
					descending = descending
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
		public void GetKeyDataRecords(KeyDataRecordsRequest request, Action<KeyDataRecords> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<KeyDataRecordsRequest,KeyDataRecords>("GetKeyDataRecords", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetKeyDataRecords(string record_type, string parent, Action<KeyDataRecords> onSuccess, OnErrorReply onErr)
		{
			GetKeyDataRecords(
            new KeyDataRecordsRequest
				{
					record_type = record_type,
					parent = parent
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
		public void GetHostWithResident(GetHostWithResidentRequest request, Action<GetHostWithResidentReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<GetHostWithResidentRequest,GetHostWithResidentReply>("GetHostWithResident", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetHostWithResident(string resident_id, Action<GetHostWithResidentReply> onSuccess, OnErrorReply onErr)
		{
			GetHostWithResident(
            new GetHostWithResidentRequest
				{
					resident_id = resident_id
				},
				onSuccess,
                onErr
           ); 
		}
		public void CreateResident(CreateResidentRequest request, Action<CreateResidentReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<CreateResidentRequest,CreateResidentReply>("CreateResident", request,
				onSuccess,
				onErr
            ); 
		}

		public void CreateResident(string resident_type, Dictionary<string,object> values, Action<CreateResidentReply> onSuccess, OnErrorReply onErr)
		{
			CreateResident(
            new CreateResidentRequest
				{
					resident_type = resident_type,
					values = values
				},
				onSuccess,
                onErr
           ); 
		}
		public void DeleteResident(DeleteResidentRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<DeleteResidentRequest,EmptyClientReply>("DeleteResident", request,
				onSuccess,
				onErr
            ); 
		}

		public void DeleteResident(string resident_id, Dictionary<string,object> final_values, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			DeleteResident(
            new DeleteResidentRequest
				{
					resident_id = resident_id,
					final_values = final_values
				},
				onSuccess,
                onErr
           ); 
		}
		public void RemoveResidentFromAllChannels(RemoveResidentFromAllChannelsRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<RemoveResidentFromAllChannelsRequest,EmptyClientReply>("RemoveResidentFromAllChannels", request,
				onSuccess,
				onErr
            ); 
		}

		public void RemoveResidentFromAllChannels(string resident_id, Dictionary<string,object> final_values, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			RemoveResidentFromAllChannels(
            new RemoveResidentFromAllChannelsRequest
				{
					resident_id = resident_id,
					final_values = final_values
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

		public void SetTransientValue(string resident_id, string key, object value, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			SetTransientValue(
            new SetTransientValueRequest
				{
					resident_id = resident_id,
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

		public void SetTransientValues(string resident_id, Dictionary<string,object> values, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			SetTransientValues(
            new SetTransientValuesRequest
				{
					resident_id = resident_id,
					values = values
				},
				onSuccess,
                onErr
           ); 
		}
		public void DeleteTransientValue(DeleteTransientValueRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<DeleteTransientValueRequest,EmptyClientReply>("DeleteTransientValue", request,
				onSuccess,
				onErr
            ); 
		}

		public void DeleteTransientValue(string resident_id, string key, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			DeleteTransientValue(
            new DeleteTransientValueRequest
				{
					resident_id = resident_id,
					key = key
				},
				onSuccess,
                onErr
           ); 
		}
		public void DeleteTransientValues(DeleteTransientValuesRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<DeleteTransientValuesRequest,EmptyClientReply>("DeleteTransientValues", request,
				onSuccess,
				onErr
            ); 
		}

		public void DeleteTransientValues(string resident_id, List<string> values, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			DeleteTransientValues(
            new DeleteTransientValuesRequest
				{
					resident_id = resident_id,
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

		public void GetTransientValue(string resident_id, string key, Action<TransientDataItemReply> onSuccess, OnErrorReply onErr)
		{
			GetTransientValue(
            new GetTransientValueRequest
				{
					resident_id = resident_id,
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

		public void GetTransientValues(string resident_id, List<string> keys, Action<TransientDataItemsReply> onSuccess, OnErrorReply onErr)
		{
			GetTransientValues(
            new GetTransientValuesRequest
				{
					resident_id = resident_id,
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

		public void GetAllTransientValues(string resident_id, Action<TransientDataItemsReply> onSuccess, OnErrorReply onErr)
		{
			GetAllTransientValues(
            new GetAllTransientValuesRequest
				{
					resident_id = resident_id
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

		public void JoinChannel(string channel_id, string resident_id, Dictionary<string,object> values, Action<ChannelInfo> onSuccess, OnErrorReply onErr)
		{
			JoinChannel(
            new JoinChannelRequest
				{
					channel_id = channel_id,
					resident_id = resident_id,
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

		public void LeaveChannel(string channel_id, string resident_id, Dictionary<string,object> final_values, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			LeaveChannel(
            new LeaveChannelRequest
				{
					channel_id = channel_id,
					resident_id = resident_id,
					final_values = final_values
				},
				onSuccess,
                onErr
           ); 
		}
		public void AddResidentToChannel(JoinChannelRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<JoinChannelRequest,EmptyClientReply>("AddResidentToChannel", request,
				onSuccess,
				onErr
            ); 
		}

		public void AddResidentToChannel(string channel_id, string resident_id, Dictionary<string,object> values, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			AddResidentToChannel(
            new JoinChannelRequest
				{
					channel_id = channel_id,
					resident_id = resident_id,
					values = values
				},
				onSuccess,
                onErr
           ); 
		}
		public void RemoveResidentFromChannel(LeaveChannelRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<LeaveChannelRequest,EmptyClientReply>("RemoveResidentFromChannel", request,
				onSuccess,
				onErr
            ); 
		}

		public void RemoveResidentFromChannel(string channel_id, string resident_id, Dictionary<string,object> final_values, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			RemoveResidentFromChannel(
            new LeaveChannelRequest
				{
					channel_id = channel_id,
					resident_id = resident_id,
					final_values = final_values
				},
				onSuccess,
                onErr
           ); 
		}
		public void ListenToChannel(ListenToChannelRequest request, Action<ChannelInfo> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<ListenToChannelRequest,ChannelInfo>("ListenToChannel", request,
				onSuccess,
				onErr
            ); 
		}

		public void ListenToChannel(string channel_id, Action<ChannelInfo> onSuccess, OnErrorReply onErr)
		{
			ListenToChannel(
            new ListenToChannelRequest
				{
					channel_id = channel_id
				},
				onSuccess,
                onErr
           ); 
		}
		public void StopListenToChannel(StopListenToChannelRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<StopListenToChannelRequest,EmptyClientReply>("StopListenToChannel", request,
				onSuccess,
				onErr
            ); 
		}

		public void StopListenToChannel(string channel_id, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			StopListenToChannel(
            new StopListenToChannelRequest
				{
					channel_id = channel_id
				},
				onSuccess,
                onErr
           ); 
		}
		public void TriggerResidentEvent(TriggerResidentEventRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<TriggerResidentEventRequest,EmptyClientReply>("TriggerResidentEvent", request,
				onSuccess,
				onErr
            ); 
		}

		public void TriggerResidentEvent(string resident_id, string event_type, string event_data, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			TriggerResidentEvent(
            new TriggerResidentEventRequest
				{
					resident_id = resident_id,
					event_type = event_type,
					event_data = event_data
				},
				onSuccess,
                onErr
           ); 
		}
		public void SendUserMessage(SendUserMessageRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<SendUserMessageRequest,EmptyClientReply>("SendUserMessage", request,
				onSuccess,
				onErr
            ); 
		}

		public void SendUserMessage(string to, string message_type, string message, bool guaranteed, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			SendUserMessage(
            new SendUserMessageRequest
				{
					to = to,
					message_type = message_type,
					message = message,
					guaranteed = guaranteed
				},
				onSuccess,
                onErr
           ); 
		}
		public void GetPendingMessages(EmptyClientRequest request, Action<UserMessageList> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<EmptyClientRequest,UserMessageList>("GetPendingMessages", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetPendingMessages(Action<UserMessageList> onSuccess, OnErrorReply onErr)
		{
			GetPendingMessages(
            new EmptyClientRequest
				{

				},
				onSuccess,
                onErr
           ); 
		}
		public void ClearPendingMessage(ClearMessageRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<ClearMessageRequest,EmptyClientReply>("ClearPendingMessage", request,
				onSuccess,
				onErr
            ); 
		}

		public void ClearPendingMessage(string id, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			ClearPendingMessage(
            new ClearMessageRequest
				{
					id = id
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
		public void GetProducts(GetProductsRequest request, Action<ProductInfoList> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<GetProductsRequest,ProductInfoList>("GetProducts", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetProducts(Action<ProductInfoList> onSuccess, OnErrorReply onErr)
		{
			GetProducts(
            new GetProductsRequest
				{

				},
				onSuccess,
                onErr
           ); 
		}
		public void GetProduct(GetProductRequest request, Action<ProductInfo> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<GetProductRequest,ProductInfo>("GetProduct", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetProduct(string product_id, Action<ProductInfo> onSuccess, OnErrorReply onErr)
		{
			GetProduct(
            new GetProductRequest
				{
					product_id = product_id
				},
				onSuccess,
                onErr
           ); 
		}
		public void stripeCheckout(StripeCheckoutRequest request, Action<ProductPurchasedReply> onSuccess, OnErrorReply onErr)
		{
            
			ApiByName<StripeCheckoutRequest,ProductPurchasedReply>("stripeCheckout", request,
				onSuccess,
				onErr
            ); 
		}

		public void stripeCheckout(string stripe_token, string product_id, Action<ProductPurchasedReply> onSuccess, OnErrorReply onErr)
		{
			stripeCheckout(
            new StripeCheckoutRequest
				{
					stripe_token = stripe_token,
					product_id = product_id
				},
				onSuccess,
                onErr
           ); 
		}


	}

}