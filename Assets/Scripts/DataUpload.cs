using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DataUpload : MonoBehaviour {
	//temp task sheets url
	public static string dataURL = "https://script.google.com/macros/s/AKfycbwyINAMK-cPZFC5fowl06-6nwxhRxb6ke--MardwIgKQL04eSKPbbeGgAGb61mrYtD45A/exec";

	void Update() {
		if (Input.GetKeyDown(KeyCode.U)) {
			SendScore("score", 123123);
		}
	}
	public void SendScore(string playerName, int score) {
		StartCoroutine(PostScore(playerName, score));
	}
	IEnumerator PostScore(string playerName, int score) {
		WWWForm form = new WWWForm();
		form.AddField("name", playerName);
		form.AddField("score", score.ToString());

		using (UnityWebRequest www = UnityWebRequest.Post(dataURL, form)) {
			yield return www.SendWebRequest();

			if (www.result != UnityWebRequest.Result.Success) {
				Debug.Log(www.error);
			} else {
				Debug.Log("Form upload complete!");
				Debug.Log(www.downloadHandler.text);
			}
		}
	}
}
