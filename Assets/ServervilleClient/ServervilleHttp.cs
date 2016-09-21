using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Text;
using System;

namespace Serverville
{

	class ServervilleHttpComponent : MonoBehaviour 
	{
		public static ServervilleHttpComponent Get()
		{
			GameObject obj = GameObject.Find("/Serverville");
			if(obj == null)
			{
				obj = new GameObject("Serverville");
				DontDestroyOnLoad(obj);
			}

			ServervilleHttpComponent http = obj.GetComponent<ServervilleHttpComponent>();
			if(http == null)
			{
				http = obj.AddComponent<ServervilleHttpComponent>();
			}

			return http;
		}
			
		public IEnumerator PostJSON(ServervilleClient sv, string url, string request, Type replyType, string sessionId, Action<object> onSuccess, OnErrorReply onError)
		{
			using(UnityWebRequest www = new UnityWebRequest(url))
			{
				www.method = UnityWebRequest.kHttpVerbPOST;

				UploadHandlerRaw upload = new UploadHandlerRaw(Encoding.UTF8.GetBytes(request));
				upload.contentType = "application/json";
				www.uploadHandler = upload;
				www.disposeUploadHandlerOnDispose = true;
			
				DownloadHandlerBuffer buffer = new DownloadHandlerBuffer();
				www.downloadHandler = buffer;
				www.disposeDownloadHandlerOnDispose = true;

				if(sessionId != null)
					www.SetRequestHeader("Authorization", sessionId);

				if(sv.LogMessages)
					Debug.Log("HTTP<- "+request);

				yield return www.Send();

				if(www.isError)
				{
					ErrorReply err = ErrorReply.makeClientErrorCode(-2, www.error);
					if(onError != null)
						onError(err);

					yield break;
				}

				if(sv.LogMessages)
					Debug.Log("HTTP-> "+buffer.text);

				if(www.responseCode >= 200 && www.responseCode < 400)
				{
					object reply = JsonConvert.DeserializeObject(buffer.text, replyType, ServervilleHttp.JsonSettings);
					if(onSuccess != null)
						onSuccess(reply);
				}
				else
				{
					ErrorReply err = JsonConvert.DeserializeObject<ErrorReply>(buffer.text, ServervilleHttp.JsonSettings);
					sv.OnServerError(err);
					if(onError != null)
						onError(err);
				}
			}
		}
	}

	public class ServervilleHttp : ServervilleTransport
	{
		private ServervilleClient SV;

		private ServervilleHttpComponent MonoComponent;
		public static JsonSerializerSettings JsonSettings;

		public ServervilleHttp(ServervilleClient sv)
		{
			SV = sv;
		}

		public void Init(OnErrorReply onConnected)
		{
			MonoComponent = ServervilleHttpComponent.Get();

			GetSerializerSettings();

			if(onConnected != null)
				onConnected(null);
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
			string json = JsonConvert.SerializeObject(request, Formatting.None, JsonSettings);

			string url = SV.ServerURL+"/api/"+api;

			MonoComponent.StartCoroutine(MonoComponent.PostJSON(SV, url, json, typeof(ReplyType), SV.SessionId,
				delegate(object reply)
				{
					if(onSuccess != null)
						onSuccess((ReplyType)reply);
				},
				onError));
		}

		public void Close()
		{
			
		}

	}

}
