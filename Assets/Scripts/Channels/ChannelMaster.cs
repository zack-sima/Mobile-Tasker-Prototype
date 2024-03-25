using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ChannelMaster : MonoBehaviour {
	public static ChannelMaster instance;

	//prefabs
	[SerializeField] private GameObject channelRowPrefab;

	//references
	[SerializeField] private TMP_InputField newChannelNameInput;
	[SerializeField] private RectTransform scrollViewContent;

	//members
	private ChannelList myChannels = new(); //saved in PlayerPrefs
	private List<ChannelRow> channelRows = new();

	private void Awake() {
		instance = this;
	}
	void Start() {
		string myChannelsJson = PlayerPrefs.GetString("channels_list");
		try {
			myChannels = (ChannelList)MyJsonUtility.FromJson(typeof(ChannelList), myChannelsJson);
			myChannels.channelIds ??= new();
		} catch {
			myChannels.channelIds ??= new();
		}
		RenderChannels();
		StartCoroutine(GetChannelIDs());
	}
	public void AddChannel() {
		string channelName = newChannelNameInput.text;
		if (channelName == "") channelName = "New Channel";

		string channelId = System.DateTime.UtcNow.ToBinary().ToString() + "_" + Random.Range(0, 10000);

		myChannels.channelIds.Add(channelId);

		ChannelSaveLoad.CreateNewEmptyChannel(channelId, channelName);

		SaveChannelIds();

	}
	public void RemoveChannel(string channelId) {
		if (myChannels.channelIds.Contains(channelId))
			myChannels.channelIds.Remove(channelId);

		SaveChannelIds();
		RenderChannels();
	}
	public void GoToChannel(string channelId) {
		PlayerPrefs.SetString("current_channel", channelId);
		SceneManager.LoadScene(1);
	}
	private void SaveChannelIds() {
		PlayerPrefs.SetString("channels_list", MyJsonUtility.ToJson(typeof(ChannelList), myChannels));
		RenderChannels();
	}
	private void RenderChannels() {
		//delete existing backup rows to clean up
		foreach (ChannelRow r in channelRows) {
			Destroy(r.gameObject);
		}
		channelRows.Clear();

		//create new rows in backup scroll view
		foreach (string s in myChannels.channelIds) {
			ChannelRow r = Instantiate(channelRowPrefab, scrollViewContent).GetComponent<ChannelRow>();
			ChannelSaveLoad.ChannelData d = ChannelSaveLoad.GetChannelData(s);

			r.SetChannel(s);
			if (d == null || d.channelName == "") {
				r.SetTitle("Untitled Channel");
			} else {
				r.SetTitle(d.channelName);
			}
			channelRows.Add(r);
		}
	}

	//GPT-4 generated
	IEnumerator GetChannelIDs() {
		UnityWebRequest www = UnityWebRequest.Get(DataUpload.dataURL + "?action=get_channels");
		yield return www.SendWebRequest();

		if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError) {
			Debug.LogError("Error: " + www.error);
		} else {
			// Show results as text
			Debug.Log("Response: " + www.downloadHandler.text);

			// Process JSON data
			try {
				// Assuming the response is a JSON array of strings like ["channel1", "channel2", ...]
				string jsonString = "{\"channelIds\":" + www.downloadHandler.text + "}";
				ChannelList channelList = (ChannelList)MyJsonUtility.FromJson(
					typeof(ChannelList), jsonString);

				List<string> channelIds = channelList.channelIds;

				//append to existing channel ids
				foreach (string channelId in channelIds) {
					StartCoroutine(GetDataCoroutine(channelId));
				}
				SaveChannelIds();
			} catch (System.Exception ex) {
				Debug.LogError("JSON Parsing Error: " + ex.Message);
			}
		}
	}
	IEnumerator GetDataCoroutine(string channelId) {
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
					rawData = DataDownload.SanatizeJson(rawData);

					ChannelSaveLoad.ChannelData d = (ChannelSaveLoad.ChannelData)MyJsonUtility.FromJson(
						typeof(ChannelSaveLoad.ChannelData), rawData);
					PlayerPrefs.SetString("channel_data_" + channelId, d.ToString());

					if (!myChannels.channelIds.Contains(channelId)) {
						myChannels.channelIds.Add(channelId);
						SaveChannelIds();
					}
				}
			}
		}
	}
	[System.Serializable]
	private class ChannelList {
		public List<string> channelIds;
	}
}
