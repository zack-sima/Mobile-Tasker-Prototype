using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DataUpload : MonoBehaviour {
	//temp task sheets url
	public static string dataURL = "https://script.google.com/macros/s/AKfycbwyINAMK-cPZFC5fowl06-6nwxhRxb6ke--MardwIgKQL04eSKPbbeGgAGb61mrYtD45A/exec";

	public void SendData(string playerName, int score) {
		StartCoroutine(PostData(playerName, score));
	}
	IEnumerator PostData(string playerName, int score) {
		BlockMaster.instance.SetLoadingScreen(true);

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
		BlockMaster.instance.SetLoadingScreen(false);
	}
}
