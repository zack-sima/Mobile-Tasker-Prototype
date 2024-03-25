using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DataUpload : MonoBehaviour {
	//temp task sheets url
	public static string dataURL = "https://script.google.com/macros/s/AKfycby9swBn0u04HVe7tvQdDwOlUpp4mdPWgE77P3tF9pwbcJiRifzWpCEHNyKbBCjEg6OCtw/exec";

	public void SendData(string channelId, string jsonData) {
		StartCoroutine(PostData(channelId, jsonData));
	}
	IEnumerator PostData(string channelId, string jsonData) {
		//BlockMaster.instance.SetLoadingScreen(true);

		WWWForm form = new WWWForm();
		form.AddField("channelId", channelId);
		form.AddField("jsonData", jsonData);

		using (UnityWebRequest www = UnityWebRequest.Post(dataURL, form)) {
			yield return www.SendWebRequest();

			if (www.result != UnityWebRequest.Result.Success) {
				Debug.Log(www.error);
			} else {
				Debug.Log("Form upload complete!");
				Debug.Log(www.downloadHandler.text);
			}
		}
		//BlockMaster.instance.SetLoadingScreen(false);
	}
}
