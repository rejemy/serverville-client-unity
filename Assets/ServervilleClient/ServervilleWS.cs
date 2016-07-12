#if (UNITY_EDITOR || !(UNITY_WEBPLAYER || UNITY_WEBGL))

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using System.Collections;
using System.Text;
using System;
using WebSocketSharp;
using WebSocketSharp.Net;
using System.Collections.Generic;

namespace Serverville
{
	class ServervilleWSComponent : MonoBehaviour 
	{

		public static ServervilleWSComponent Get()
		{
			GameObject obj = GameObject.Find("/Serverville");
			if(obj == null)
			{
				obj = new GameObject("Serverville");
				DontDestroyOnLoad(obj);
			}

			ServervilleWSComponent ws = obj.GetComponent<ServervilleWSComponent>();
			if(ws == null)
			{
				ws = obj.AddComponent<ServervilleWSComponent>();
			}

			return ws;
		}

		public delegate void UpdateEventHandler();
		public event UpdateEventHandler UpdateEvent;

		public void Update()
		{
			if(UpdateEvent != null)
			{
				UpdateEvent();
			}
		}
	}

	public class ServervilleWS : ServervilleTransport
	{
		private ServervilleClient SV;
		private WebSocket ServerSocket;
		private int MessageSequence = 0;

		private delegate void MessageReplyClosure(bool isError, string replyJson);
		private Dictionary<string,MessageReplyClosure> ReplyCallbacks;

		private static JsonSerializerSettings JsonSettings;

		private Queue<string> ReplyQueue;
		private Queue<string> SwapQueue;
		private object QueueLock = new object();

		public ServervilleWS(ServervilleClient sv)
		{
			SV = sv;

			ReplyQueue = new Queue<string>();
			SwapQueue = new Queue<string>();
		}

		public void Init(OnErrorReply onConnected)
		{
			GetSerializerSettings();

			ServervilleWSComponent wsComp = ServervilleWSComponent.Get();
			wsComp.UpdateEvent += Update;

			string url = SV.ServerURL+"/websocket";
			ServerSocket = new WebSocket(url);

			ReplyCallbacks = new Dictionary<string,MessageReplyClosure>();

			ServerSocket.OnOpen += (object sender, EventArgs e) => 
			{
				if(onConnected != null)
					onConnected(null);
			};

			ServerSocket.OnClose += OnWSClosed;

			ServerSocket.OnMessage += OnWSMessage;

			ServerSocket.OnError += (object sender, ErrorEventArgs e) => 
			{
				Debug.Log("Connection error: "+e.ToString());
				if(onConnected != null)
					onConnected(ErrorReply.makeClientErrorCode(-2, e.Message));
			};

			ServerSocket.Connect();
		}

		public static JsonSerializerSettings GetSerializerSettings()
		{
			if(JsonSettings == null)
			{
				JsonSettings = new JsonSerializerSettings();
				JsonSettings.Converters.Add(new StringEnumConverter());
			}

			return JsonSettings;
		}

		public void CallAPI<ReplyType>(string api, object request, Action<ReplyType> onSuccess, OnErrorReply onError)
		{
			string messageNum = (MessageSequence++).ToString();
			string json = JsonConvert.SerializeObject(request, Formatting.None, JsonSettings);

			string message = api+":"+messageNum+":"+json;

			MessageReplyClosure callback = delegate(bool isError, string replyJson)
			{
				if(isError)
				{
					ErrorReply err = JsonConvert.DeserializeObject<ErrorReply>(replyJson, JsonSettings);
					SV.OnServerError(err);
					if(onError != null)
						onError(err);
				}
				else
				{
					ReplyType reply = JsonConvert.DeserializeObject<ReplyType>(replyJson, JsonSettings);
					if(onSuccess != null)
						onSuccess(reply);
				}
			};

			ReplyCallbacks.Add(messageNum, callback);

			ServerSocket.Send(message);
		}

		public void Close()
		{
			if(ServerSocket != null)
			{
				ServerSocket.Close();
			}
		}

		// Called on a background thread
		private void OnWSClosed(object sender, CloseEventArgs e)
		{
			Debug.Log("Connection closed");
			SV.OnTransportClosed();
		}

		// Called on a background thread
		private void OnWSMessage(object sender, MessageEventArgs evt)
		{
			if(evt.IsPing)
				return;

			string messageStr = evt.Data;

			lock(QueueLock)
			{
				ReplyQueue.Enqueue(messageStr);
			}
		}

		private void Update()
		{
			if(ReplyQueue.Count == 0)
				return;

			lock(QueueLock)
			{
				Queue<string> swap = ReplyQueue;
				ReplyQueue = SwapQueue;
				SwapQueue = swap;
			}

			foreach(string message in SwapQueue)
			{
				HandleStringMessage(message);
			}
			SwapQueue.Clear();
		}

		private void HandleStringMessage(string messageStr)
		{
			try
			{
				int split1 = messageStr.IndexOf(':');
				if(split1 < 1)
				{
					Debug.Log("Incorrectly formatted message");
					return;
				}

				string messageType = messageStr.Substring(0, split1);
				if(messageType == "M")
				{
					// Server push message
					int split2 = messageStr.IndexOf(':', split1+1);
					if(split2 < 0)
					{
						Debug.Log("Incorrectly formatted message");
						return;
					}
					int split3 = messageStr.IndexOf(':', split2+1);
					if(split3 < 0)
					{
						Debug.Log("Incorrectly formatted message");
						return;
					}
					int split4 = messageStr.IndexOf(':', split3+1);
					if(split4 < 0)
					{
						Debug.Log("Incorrectly formatted message");
						return;
					}

					string messageId = messageStr.Substring(split1+1, split2-(split1+1));
					string messageFrom = messageStr.Substring(split2+1, split3-(split2+1));
					string messageVia = messageStr.Substring(split3+1, split4-(split3+1));
					string messageJson = messageStr.Substring(split4+1);

					SV.OnServerMessage(messageId, messageFrom, messageVia, messageJson);
				}
				else if(messageType == "E" || messageType == "R")
				{
					// Reply
					int split2 = messageStr.IndexOf(':', split1+1);
					if(split2 < 0)
					{
						Debug.Log("Incorrectly formatted message");
						return;
					}

					string messageNum = messageStr.Substring(split1+1, split2-(split1+1));

					string messageJson = messageStr.Substring(split2+1);

					bool isError = false;
					if(messageType == "E")
						isError = true;

					MessageReplyClosure callback = ReplyCallbacks[messageNum];
					ReplyCallbacks.Remove(messageNum);

					callback(isError, messageJson);
				}
			}
			catch(Exception exc)
			{
				// Error handling message
				Debug.Log("Error handling message: "+exc.ToString());
			}
		}
	}

}
#else

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using System.Collections;
using System.Text;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Serverville
{

	public class ServervilleWS : ServervilleTransport
	{
		private ServervilleClient SV;
		private WebSocketWrapper ServerSocket;
		private int MessageSequence = 0;

		private delegate void MessageReplyClosure(bool isError, string replyJson);
		private Dictionary<string,MessageReplyClosure> ReplyCallbacks;

		public static JsonSerializerSettings JsonSettings;

		public ServervilleWS(ServervilleClient sv)
		{
			SV = sv;

		}

		public void Init(OnErrorReply onConnected)
		{

			string url = SV.ServerURL+"/websocket";
			ServerSocket = WebSocketWrapper.Create(url);

			ReplyCallbacks = new Dictionary<string,MessageReplyClosure>();

			ServerSocket.OpenEvent += () => 
			{
				if(onConnected != null)
					onConnected(null);
			};

			ServerSocket.CloseEvent += OnWSClosed;

			ServerSocket.StringMessageEvent += HandleStringMessage;

			ServerSocket.ErrorEvent += (message) => 
			{
				Debug.Log("Connection error: "+message);
				if(onConnected != null)
					onConnected(ErrorReply.makeClientErrorCode(1, message));
			};

			ServerSocket.Connect();
		}

		public void CallAPI<ReplyType>(string api, object request, Action<ReplyType> onSuccess, OnErrorReply onError)
		{
			string messageNum = (MessageSequence++).ToString();
			string json = JsonConvert.SerializeObject(request, Formatting.None, JsonSettings);

			string message = api+":"+messageNum+":"+json;

			MessageReplyClosure callback = delegate(bool isError, string replyJson)
			{
				if(isError)
				{
					ErrorReply err = JsonConvert.DeserializeObject<ErrorReply>(replyJson, ServervilleHttp.JsonSettings);
					if(onError != null)
						onError(err);
				}
				else
				{
					ReplyType reply = JsonConvert.DeserializeObject<ReplyType>(replyJson, ServervilleHttp.JsonSettings);
					if(onSuccess != null)
						onSuccess(reply);
				}
			};

			ReplyCallbacks.Add(messageNum, callback);

			ServerSocket.Send(message);
		}
		
		public void Close()
		{
			if(ServerSocket != null)
			{
				ServerSocket.Close();
			}
		}

		private void OnWSClosed(int code)
		{
			Debug.Log("Connection closed");
			SV.OnTransportClosed();
		}
			
		private void OnWSBinaryMessage()
		{
		}

		private void HandleStringMessage(string messageStr)
		{
			try
			{
				int split1 = messageStr.IndexOf(':');
				if(split1 < 1)
				{
					Debug.Log("Incorrectly formatted message");
					return;
				}

				string messageType = messageStr.Substring(0, split1);
				if(messageType == "M")
				{
					// Server push message
					int split2 = messageStr.IndexOf(':', split1+1);
					if(split2 < 0)
					{
						Debug.Log("Incorrectly formatted message");
						return;
					}
					int split3 = messageStr.IndexOf(':', split2+1);
					if(split3 < 0)
					{
						Debug.Log("Incorrectly formatted message");
						return;
					}
					int split4 = messageStr.IndexOf(':', split3+1);
					if(split4 < 0)
					{
						Debug.Log("Incorrectly formatted message");
						return;
					}

					string messageId = messageStr.Substring(split1+1, split2-(split1+1));
					string messageFrom = messageStr.Substring(split2+1, split3-(split2+1));
					string messageVia = messageStr.Substring(split3+1, split4-(split3+1));
					string messageJson = messageStr.Substring(split4+1);

					SV.OnServerMessage(messageId, messageFrom, messageVia, messageJson);
				}
				else if(messageType == "E" || messageType == "R")
				{
					// Reply
					int split2 = messageStr.IndexOf(':', split1+1);
					if(split2 < 0)
					{
						Debug.Log("Incorrectly formatted message");
						return;
					}

					string messageNum = messageStr.Substring(split1+1, split2-(split1+1));

					string messageJson = messageStr.Substring(split2+1);

					bool isError = false;
					if(messageType == "E")
						isError = true;

					MessageReplyClosure callback = ReplyCallbacks[messageNum];
					ReplyCallbacks.Remove(messageNum);

					callback(isError, messageJson);
				}
			}
			catch(Exception exc)
			{
				// Error handling message
				Debug.Log("Error handling message: "+exc.ToString());
			}
		}
	}


	public class WebSocketWrapper : MonoBehaviour 
	{
		private static int NextId = 1;

		private int SocketId;
		private string _URL;

		public delegate void OnWebsocketOpenHandler();
		public event OnWebsocketOpenHandler OpenEvent;

		public delegate void OnWebsocketStringMessageHandler(string message);
		public event OnWebsocketStringMessageHandler StringMessageEvent;

		public delegate void OnWebsocketErrorHandler(string message);
		public event OnWebsocketErrorHandler ErrorEvent;

		public delegate void OnWebsocketCloseHandler(int code);
		public event OnWebsocketCloseHandler CloseEvent;

		public static WebSocketWrapper Create(string url)
		{
			int id = NextId++;

			GameObject obj = new GameObject("ServervilleWSHandler"+id);
			DontDestroyOnLoad(obj);

			WebSocketWrapper ws = obj.AddComponent<WebSocketWrapper>();
			ws.SocketId = id;

			ws._URL = url;

			return ws;
		}

		public void Connect()
		{
			Serverville_SocketCreate(SocketId, _URL);
		}

		public void Close()
		{
			Serverville_SocketClose(SocketId);
		}

		public void Send(string str)
		{
			Serverville_SocketSendString(SocketId, str);
		}

		public void Send(byte[] buffer)
		{
			Serverville_SocketSendBinary(SocketId, buffer, buffer.Length);
		}

		public void Dispose()
		{
			Serverville_SocketDispose(SocketId);
			GameObject.Destroy(gameObject);
		}

		public string URL
		{
			get
			{
				return Serverville_SocketURL(SocketId);
			}
		}

		public string Protocol
		{
			get
			{
				return Serverville_SocketProtocol(SocketId);
			}
		}

		public int ReadyState
		{
			get
			{
				return Serverville_SocketReadyState(SocketId);
			}
		}

		public string BinaryType
		{
			get
			{
				return Serverville_SocketBinaryType(SocketId);
			}
		}

		public string Extensions
		{
			get
			{
				return Serverville_SocketExtensions(SocketId);
			}
		}

		public ulong BufferedAmount
		{
			get
			{
				return Serverville_SocketBufferedAmount(SocketId);
			}
		}

		public void OnWebsocketOpen(String eventType)
		{
			if(OpenEvent != null)
				OpenEvent();
		}

		public void OnWebsocketStringMessage(String data)
		{
			if(StringMessageEvent != null)
				StringMessageEvent(data);
		}

		public void OnWebsocketError(String message)
		{
			if(ErrorEvent != null)
				ErrorEvent(message);
		}

		public void OnWebsocketClose(int code)
		{
			if(CloseEvent != null)
				CloseEvent(code);
		}


		[DllImport("__Internal")]
		private static extern void Serverville_SocketCreate(int socketId, string url);

		[DllImport("__Internal")]
		private static extern int Serverville_SocketReadyState(int socketId);

		[DllImport("__Internal")]
		private static extern string Serverville_SocketURL(int socketId);

		[DllImport("__Internal")]
		private static extern string Serverville_SocketProtocol(int socketId);

		[DllImport("__Internal")]
		private static extern string Serverville_SocketBinaryType(int socketId);

		[DllImport("__Internal")]
		private static extern string Serverville_SocketExtensions(int socketId);

		[DllImport("__Internal")]
		private static extern ulong Serverville_SocketBufferedAmount(int socketId);

		[DllImport("__Internal")]
		private static extern void Serverville_SocketSendString(int socketId, string str);

		[DllImport("__Internal")]
		private static extern void Serverville_SocketSendBinary(int socketId, byte[] ptr, int length);

		[DllImport("__Internal")]
		private static extern void Serverville_SocketClose(int socketId);

		[DllImport("__Internal")]
		private static extern string Serverville_SocketDispose(int socketId);

	}
}
#endif