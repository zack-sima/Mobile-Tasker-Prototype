using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DataUpload : MonoBehaviour {
	public static DataUpload instance;

	//temp task sheets url
	public static string dataURL = "https://script.google.com/macros/s/AKfycbx5S478iOL3Fim9CT8NjDPfDjTZADEhXOUH1mgpuZG0pWtC9o2fNnfVahbzgrBUzhX46A/exec";

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
