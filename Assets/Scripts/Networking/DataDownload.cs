using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DataDownload : MonoBehaviour {
	public void GetData() {
		StartCoroutine(GetDataCoroutine());
	}
	IEnumerator GetDataCoroutine() {
		BlockMaster.instance.SetLoadingScreen(true);
		using (UnityWebRequest www = UnityWebRequest.Get(DataUpload.dataURL)) {
			yield return www.SendWebRequest();

			if (www.result != UnityWebRequest.Result.Success) {
				Debug.Log(www.error);
			} else {
				Debug.Log(www.downloadHandler.text);
				// Here you can convert the JSON response to your desired format and use it in your game
			}
		}
		BlockMaster.instance.SetLoadingScreen(false);
	}
}
