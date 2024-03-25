using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ChannelRow : MonoBehaviour {
	[SerializeField] private TMP_Text channelDisplay;

	private string channelName = "";

	public void SetTitle(string title) {
		channelDisplay.text = title;
	}
	public void SetChannel(string channel) {
		channelName = channel;
	}
	public void GoToChannel() {
		ChannelMaster.instance.GoToChannel(channelName);
	}
	public void DeleteChannel() {
		//TODO: double check + delete on server, etc but ONLY for channel owner
		ChannelMaster.instance.RemoveChannel(channelName);
	}
}
