using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DataDownload : MonoBehaviour {
	public void GetData(string channelId) {
		StartCoroutine(GetDataCoroutine(channelId));
	}
	IEnumerator GetDataCoroutine(string channelId) {
		BlockMaster.instance.SetLoadingScreen(true);

		using (UnityWebRequest www = UnityWebRequest.Get(
			DataUpload.dataURL + $"?channelId={channelId}")) {

			yield return www.SendWebRequest();

			if (www.result != UnityWebRequest.Result.Success) {
				Debug.Log(www.error);
			} else {
				Debug.Log(www.downloadHandler.text);

				// Here you can convert the JSON response to your desired format and use it in your game
				string rawData = www.downloadHandler.text;
				if (rawData.Length == 0 || rawData.Length < 30 && rawData.Contains("not found")) {
					Debug.LogWarning("could not retrieve channel!");
				} else {
					rawData = rawData[2..^2];
					rawData = rawData.Replace(@"\\n", "\n");
					rawData = rawData.Replace(@"\\t", "\t");
					rawData = rawData.Replace("\\\"", "\"");

					//actual " character literals
					rawData = rawData.Replace("\\\\\"", "\\\"");

					//actual \ character literals
					rawData = rawData.Replace("\\\\", "\\");

					ChannelSaveLoad.LoadChannelWithString(BlockMaster.instance, rawData);
				}
			}
		}
		BlockMaster.instance.SetLoadingScreen(false);
	}
}
