using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is the class used to save a channel's data (of blocks).
/// TODO: General scope for future -- Team -> Project -> Active/Archived -> Channel,
/// but until we stop using Google Sheets for storage just channels will have to do.
/// </summary>

public static class ChannelSaveLoad {
	[System.Serializable]
	public class SaveBlock {
		public List<SaveBlock> children;
		public bool isFolded;
		public string text;
	}
	[System.Serializable]
	public class ChannelData {
		public List<SaveBlock> blocks;
		public string channelName;

		public override string ToString() {
			return MyJsonUtility.ToJson(typeof(ChannelData), this);
		}
	}
	//TODO: channelId should be the given ID of the channel when it is created;
	//channelName can be changed and is displayed
	public static void SaveChannel(List<TaskBlock> blocks, string channelId, string channelName) {
		//TODO: save in a more coherent manner
		PlayerPrefs.SetString(channelId, CreateChannelString(blocks, channelName));
	}
	public static string CreateChannelString(List<TaskBlock> blocks, string channelName) {
		ChannelData data = new() {
			channelName = channelName,
			blocks = new()
		};
		foreach (TaskBlock b in blocks) {
			data.blocks.Add(CreateBlockWithChildren(data, b));
		}
		return data.ToString();
	}
	//helper recursive function
	private static SaveBlock CreateBlockWithChildren(ChannelData data, TaskBlock parent) {
		SaveBlock save = new() {
			children = new(),
			text = parent.GetText(),
			isFolded = parent.GetIsFolded()
		};
		foreach (TaskBlock b in parent.GetChildrenList()) {
			save.children.Add(CreateBlockWithChildren(data, b));
		}
		return save;
	}
	public static bool LoadChannel(BlockMaster master, string channelName) {
		string dataString = PlayerPrefs.GetString(channelName);
		if (dataString == "") return false;

		return LoadChannelWithString(master, dataString);
	}
	public static bool LoadChannelWithString(BlockMaster master, string dataString) {
		master.ClearData();
		ChannelData data = (ChannelData)MyJsonUtility.FromJson(typeof(ChannelData), dataString);
		if (data == null || data.blocks == null) return false;
		foreach (SaveBlock s in data.blocks) {
			master.GetBlocks().Add(LoadBlockAndAllChildren(master, s));
		}
		master.SetTitle(data.channelName);
		master.RecalculateBlocks(recheckInputs: true);
		master.ChannelLoaded();
		return true;
	}
	//helper recursive function
	private static TaskBlock LoadBlockAndAllChildren(BlockMaster master, SaveBlock parent) {
		TaskBlock newBlock = master.CreateBlock(recalculate: false);
		foreach (SaveBlock child in parent.children) {
			TaskBlock childBlock = LoadBlockAndAllChildren(master, child);
			childBlock.SetParent(newBlock);
			newBlock.GetChildrenList().Add(childBlock);
		}
		newBlock.SetIsFolded(parent.isFolded);
		newBlock.SetText(parent.text);
		return newBlock;
	}
}
