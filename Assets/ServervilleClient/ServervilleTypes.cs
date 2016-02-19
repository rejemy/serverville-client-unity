using System;

namespace Serverville
{
	public delegate void OnSignInReply(SignInReply user);
	public delegate void OnErrorReply(ErrorReply reply);
	public delegate void OnGenericReply(object reply);

	[Serializable]
	public class ErrorReply
	{
		public int errorCode;
		public string errorMessage;
		public string errorDetails;

		public static ErrorReply makeClientErrorCode(int code, string details)
		{
			ErrorReply reply = new ErrorReply();

			reply.errorCode = code;
			reply.errorMessage = "There was a connection error";
			reply.errorDetails = details;

			return reply;
		}
	}

	interface ServervilleTransport
	{
		void Init(OnErrorReply onConnected);
		void CallAPI<ReplyType>(string api, object request, Action<ReplyType> onSuccess, OnErrorReply onError);
	}
}