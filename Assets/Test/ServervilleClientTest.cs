using UnityEngine;
using System.Collections;
using Serverville;

public class ServervilleClientTest : MonoBehaviour {

	// Use this for initialization
	void Start () {

		ServervilleClient sv = new ServervilleClient("http://localhost:8000");
		sv.Init(delegate(SignInReply userInfo, ErrorReply initErr) {
			if(initErr != null)
			{
				Debug.Log("Got an error: "+initErr.errorMessage);
			}
			else if(userInfo != null)
			{
				Debug.Log("Signed in");
			}
			else
			{
				Debug.Log("Initted");
			}

			if(userInfo == null)
			{
				Debug.Log("Signing into test account");
				sv.SignIn("testuser1", null, "testuser1",
					delegate(SignInReply reply)
					{
						Debug.Log("Signed in with session "+reply.session_id);
					},
					delegate(ErrorReply err)
					{
						Debug.Log("Error signing in "+err.errorMessage);
					}
				);
			}
		});
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
