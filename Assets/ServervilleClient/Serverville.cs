using UnityEngine;
using System;
using System.Collections.Generic;


namespace Serverville
{
	
	public class ServervilleClient
	{
		public delegate void OnInitComplete(SignInReply userInfo, ErrorReply err);

		internal string ServerURL;
		internal string SessionId;

		private SignInReply UserInfo;

		public bool LogMessages = false;

		private ServervilleTransport Transport;

		public delegate void ErrorHandlerDelegate(ErrorReply err);
		public delegate void ServerMessageTypeHandlerDelegate(string from, string json);
		public delegate void ServerMessageHandlerDelegate(string messageType, string from, string json);

		public ErrorHandlerDelegate GlobalErrorHandler;
		public ServerMessageHandlerDelegate ServerMessageHandler;
		public Dictionary<string,ServerMessageTypeHandlerDelegate> ServerMessageTypeHandlers;

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

		private void SetUserInfo(SignInReply userInfo)
		{
			if(userInfo == null)
			{
				UserInfo = null;
				SessionId = null;
				PlayerPrefs.DeleteKey("Serverville"+ServerURL+"SessionId");
			}
			else
			{
				UserInfo = userInfo;
				SessionId = userInfo.session_id;
				PlayerPrefs.SetString("Serverville"+ServerURL+"SessionId", SessionId);
				PlayerPrefs.Save();
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

		public SignInReply GetUserInfo()
		{
			return UserInfo;
		}

		internal void OnServerError(ErrorReply err)
		{
			if(GlobalErrorHandler != null)
				GlobalErrorHandler(err);
		}

		internal void OnServerMessage(string messageId, string from, string jsonData)
		{
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
        public void ApiByName<ReqType,ReplyType>(string api, ReqType request, Action<ReplyType> onSuccess, OnErrorReply onErr)
		{
			Transport.CallAPI<ReplyType>(api, request,
				onSuccess,
				onErr
            ); 
		}
        
		public void SignIn(SignIn request, Action<SignInReply> onSuccess, OnErrorReply onErr)
		{
            
			Transport.CallAPI<SignInReply>("SignIn", request,
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
            
			Transport.CallAPI<SignInReply>("ValidateSession", request,
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

		public void CreateAnonymousAccount(CreateAnonymousAccount request, Action<CreateAccountReply> onSuccess, OnErrorReply onErr)
		{
            
			Transport.CallAPI<CreateAccountReply>("CreateAnonymousAccount", request,
				onSuccess,
				onErr
            ); 
		}

		public void CreateAnonymousAccount(Action<CreateAccountReply> onSuccess, OnErrorReply onErr)
		{
			CreateAnonymousAccount(
            new CreateAnonymousAccount
				{

				},
				onSuccess,
                onErr
           ); 
		}

		public void CreateAccount(CreateAccount request, Action<CreateAccountReply> onSuccess, OnErrorReply onErr)
		{
            
			Transport.CallAPI<CreateAccountReply>("CreateAccount", request,
				onSuccess,
				onErr
            ); 
		}

		public void CreateAccount(string username, string email, string password, Action<CreateAccountReply> onSuccess, OnErrorReply onErr)
		{
			CreateAccount(
            new CreateAccount
				{
					username = username,
					email = email,
					password = password
				},
				onSuccess,
                onErr
           ); 
		}

		public void ConvertToFullAccount(CreateAccount request, Action<CreateAccountReply> onSuccess, OnErrorReply onErr)
		{
            
			Transport.CallAPI<CreateAccountReply>("ConvertToFullAccount", request,
				onSuccess,
				onErr
            ); 
		}

		public void ConvertToFullAccount(string username, string email, string password, Action<CreateAccountReply> onSuccess, OnErrorReply onErr)
		{
			ConvertToFullAccount(
            new CreateAccount
				{
					username = username,
					email = email,
					password = password
				},
				onSuccess,
                onErr
           ); 
		}

		public void GetUserInfo(GetUserInfo request, Action<SignInReply> onSuccess, OnErrorReply onErr)
		{
            
			Transport.CallAPI<SignInReply>("GetUserInfo", request,
				delegate(SignInReply reply) { SetUserInfo(reply); if(onSuccess != null) { onSuccess(reply); } },
				onErr
            ); 
		}

		public void GetUserInfo(Action<SignInReply> onSuccess, OnErrorReply onErr)
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
            
			Transport.CallAPI<SetDataReply>("SetUserKey", request,
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
            
			Transport.CallAPI<SetDataReply>("SetUserKeys", request,
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
            
			Transport.CallAPI<DataItemReply>("GetUserKey", request,
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
            
			Transport.CallAPI<UserDataReply>("GetUserKeys", request,
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
            
			Transport.CallAPI<UserDataReply>("GetAllUserKeys", request,
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
            
			Transport.CallAPI<DataItemReply>("GetDataKey", request,
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
            
			Transport.CallAPI<UserDataReply>("GetDataKeys", request,
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
            
			Transport.CallAPI<UserDataReply>("GetAllDataKeys", request,
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

		public void SetTransientValue(SetTransientValueRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			Transport.CallAPI<EmptyClientReply>("SetTransientValue", request,
				onSuccess,
				onErr
            ); 
		}

		public void SetTransientValue(string key, object value, JsonDataType data_type, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			SetTransientValue(
            new SetTransientValueRequest
				{
					key = key,
					value = value,
					data_type = data_type
				},
				onSuccess,
                onErr
           ); 
		}

		public void SetTransientValues(SetTransientValuesRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			Transport.CallAPI<EmptyClientReply>("SetTransientValues", request,
				onSuccess,
				onErr
            ); 
		}

		public void SetTransientValues(List<SetTransientValueRequest> values, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			SetTransientValues(
            new SetTransientValuesRequest
				{
					values = values
				},
				onSuccess,
                onErr
           ); 
		}

		public void GetTransientValue(GetTransientValueRequest request, Action<DataItemReply> onSuccess, OnErrorReply onErr)
		{
            
			Transport.CallAPI<DataItemReply>("GetTransientValue", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetTransientValue(string id, string key, Action<DataItemReply> onSuccess, OnErrorReply onErr)
		{
			GetTransientValue(
            new GetTransientValueRequest
				{
					id = id,
					key = key
				},
				onSuccess,
                onErr
           ); 
		}

		public void GetTransientValues(GetTransientValuesRequest request, Action<UserDataReply> onSuccess, OnErrorReply onErr)
		{
            
			Transport.CallAPI<UserDataReply>("GetTransientValues", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetTransientValues(string id, List<string> keys, Action<UserDataReply> onSuccess, OnErrorReply onErr)
		{
			GetTransientValues(
            new GetTransientValuesRequest
				{
					id = id,
					keys = keys
				},
				onSuccess,
                onErr
           ); 
		}

		public void getAllTransientValues(GetAllTransientValuesRequest request, Action<UserDataReply> onSuccess, OnErrorReply onErr)
		{
            
			Transport.CallAPI<UserDataReply>("getAllTransientValues", request,
				onSuccess,
				onErr
            ); 
		}

		public void getAllTransientValues(string id, Action<UserDataReply> onSuccess, OnErrorReply onErr)
		{
			getAllTransientValues(
            new GetAllTransientValuesRequest
				{
					id = id
				},
				onSuccess,
                onErr
           ); 
		}

		public void GetChannelInfo(JoinChannelRequest request, Action<ChannelInfo> onSuccess, OnErrorReply onErr)
		{
            
			Transport.CallAPI<ChannelInfo>("GetChannelInfo", request,
				onSuccess,
				onErr
            ); 
		}

		public void GetChannelInfo(string id, bool listen_only, Action<ChannelInfo> onSuccess, OnErrorReply onErr)
		{
			GetChannelInfo(
            new JoinChannelRequest
				{
					id = id,
					listen_only = listen_only
				},
				onSuccess,
                onErr
           ); 
		}

		public void JoinChannel(JoinChannelRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			Transport.CallAPI<EmptyClientReply>("JoinChannel", request,
				onSuccess,
				onErr
            ); 
		}

		public void JoinChannel(string id, bool listen_only, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			JoinChannel(
            new JoinChannelRequest
				{
					id = id,
					listen_only = listen_only
				},
				onSuccess,
                onErr
           ); 
		}

		public void LeaveChannel(LeaveChannelRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			Transport.CallAPI<EmptyClientReply>("LeaveChannel", request,
				onSuccess,
				onErr
            ); 
		}

		public void LeaveChannel(string id, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			LeaveChannel(
            new LeaveChannelRequest
				{
					id = id
				},
				onSuccess,
                onErr
           ); 
		}

		public void SendClientMessage(TransientMessageRequest request, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
            
			Transport.CallAPI<EmptyClientReply>("SendClientMessage", request,
				onSuccess,
				onErr
            ); 
		}

		public void SendClientMessage(string to, string message_type, object value, JsonDataType data_type, Action<EmptyClientReply> onSuccess, OnErrorReply onErr)
		{
			SendClientMessage(
            new TransientMessageRequest
				{
					to = to,
					message_type = message_type,
					value = value,
					data_type = data_type
				},
				onSuccess,
                onErr
           ); 
		}



	}

}