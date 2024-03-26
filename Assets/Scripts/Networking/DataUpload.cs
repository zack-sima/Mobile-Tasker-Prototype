using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DataUpload : MonoBehaviour {
	public static DataUpload instance;

	//temp task sheets url
	public static string dataURL = "https://script.google.com/macros/s/AKfycbxhJZZ9-qOoqL1kEyfkA7ieBi7nVqoVXCPueF-7JDR8iI3A3rFB0g0DE0_e4cm7p-p08w/exec";

	public void SendData(string channelId, string jsonData) {
		StartCoroutine(PostData(channelId, jsonData));
	}
	IEnumerator PostData(string channelId, string jsonData) {
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
	}
	private void Awake() {
		if (instance != null) {
			Destroy(gameObject);
			return;
		}
		instance = this;

		//uploads across scenes
		DontDestroyOnLoad(gameObject);
	}
}
