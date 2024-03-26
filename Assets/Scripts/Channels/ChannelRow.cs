using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ChannelRow : MonoBehaviour {
	[SerializeField] private TMP_Text channelDisplay;
	[SerializeField] private Image optionsImage, loadChannelImage;
	[SerializeField] private Sprite optionsSprite, loadChannelSprite, deleteSprite, backSprite;

	//when in options buttons are different
	private bool inOptions = false;
	private string channelName = "";

	public void SetTitle(string title) {
		channelDisplay.text = title;
	}
	public void SetChannel(string channel) {
		channelName = channel;
	}
	public void GoToChannel() {
		if (inOptions) {
			// double check + delete on server, etc but ONLY for channel owner
			ChannelMaster.instance.TryRemoveChannel(channelName);
		} else {
			ChannelMaster.instance.GoToChannel(channelName);
		}
	}
	public void Options() {
		inOptions = !inOptions;

		if (inOptions) {
			optionsImage.sprite = backSprite;
			loadChannelImage.sprite = deleteSprite;
		} else {
			optionsImage.sprite = optionsSprite;
			loadChannelImage.sprite = loadChannelSprite;
		}
	}
}
