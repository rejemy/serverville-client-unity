using UnityEngine;
using System.Collections;
using Serverville;
using UnityEngine.UI;

public class ServervilleClientTest : MonoBehaviour {

	public Text ConsoleText;

	// Use this for initialization
	void Start () {

		ConsoleText.text = "";

		string url = "ws://localhost:8000";

		Log("Connecting to "+url);

		ServervilleClient sv = new ServervilleClient(url);
		sv.Init(delegate(SignInReply userInfo, ErrorReply initErr) {
			if(initErr != null)
			{
				Log("Got an error: "+initErr.errorMessage);
				return;
			}
			else if(userInfo != null)
			{
				Log("Signed in");
			}
			else
			{
				Log("Initted");
			}


			Debug.Log("Signing into test account");
			sv.SignIn("testuser1", null, "testuser1",
				delegate(SignInReply reply)
				{
					Log("Signed in with session "+reply.session_id);
				},
				delegate(ErrorReply err)
				{
					Log("Error signing in "+err.errorMessage);
				}
			);

		});
	}
	
	public void Log(string text)
	{
		Debug.Log(text);
		ConsoleText.text += text+"\n";
	}
}
