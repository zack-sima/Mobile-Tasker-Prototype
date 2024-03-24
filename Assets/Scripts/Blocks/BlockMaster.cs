using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the dragging & arrangement of blocks, and the scroll view on the Main Canvas.
/// </summary>

public class BlockMaster : MonoBehaviour {
	public static BlockMaster instance;

	#region Prefabs

	[SerializeField] private GameObject blockPrefab;

	#endregion

	#region Constants

	public const float BLOCK_SPACING = 20f; //spacing between blocks
	public const float BLOCK_TOP_PADDING = 50f; //extra padding at top

	#endregion

	#region References

	[SerializeField] private Canvas mainCanvas;
	public Canvas GetMainCanvas() { return mainCanvas; }
	public float GetCanvasScaleFactor() { return mainCanvas.scaleFactor; }

	[SerializeField] private ScrollRect scrollViewParent;
	public ScrollRect GetScrollRect() { return scrollViewParent; }

	[SerializeField] private RectTransform scrollViewContent;

	//shows where the block would go with indents when a block is dragged
	[SerializeField] private RectTransform blockShadow;

	//shows up when delete block is called to confirm
	[SerializeField] private RectTransform deleteBlockScreen, deleteAllBlocksScreen;

	[SerializeField] private RectTransform optionsMenuBar, normalMenuBar;

	[SerializeField] private RectTransform loadingScreen;
	public void SetLoadingScreen(bool isOn) { loadingScreen.gameObject.SetActive(isOn); }

	#endregion

	#region Members

	//keep track of all the parent blocks (each block has a list of its children)
	private List<TaskBlock> blocks = new();
	public List<TaskBlock> GetBlocks() { return blocks; }

	//whether to show delete button, set clock button, etc...
	private bool showOptions = false;
	public bool GetShowOptions() { return showOptions; }

	#endregion

	#region Button Callbacks

	public void ShowNormalBar() {
		optionsMenuBar.gameObject.SetActive(false);
		normalMenuBar.gameObject.SetActive(true);
	}
	public void ShowOptionsBar() {
		optionsMenuBar.gameObject.SetActive(true);
		normalMenuBar.gameObject.SetActive(false);
	}

	private TaskBlock blockToDelete = null;
	public void DeleteBlockCalled(TaskBlock from) {
		blockToDelete = from;
		deleteBlockScreen.gameObject.SetActive(true);
	}
	public void CancelDeleteBlock() {
		blockToDelete = null;
		deleteBlockScreen.gameObject.SetActive(false);
	}
	public void ActuallyDeleteBlock() {
		if (blockToDelete != null)
			blockToDelete.DeleteBlock();
		blockToDelete = null;
		deleteBlockScreen.gameObject.SetActive(false);
	}
	public void ToggleOptions() {
		showOptions = !showOptions;

		foreach (TaskBlock tb in blocks) {
			tb.ToggleShowOptions(showOptions);
		}
	}
	//called by button; creates empty block in FRONT
	public void CreateBlock() { CreateBlock(true); }
	public TaskBlock CreateBlock(bool recalculate) {
		GameObject newBlockObj = Instantiate(blockPrefab, scrollViewContent);

		if (recalculate) {
			blocks.Insert(0, newBlockObj.GetComponent<TaskBlock>());
			RecalculateBlocks();
			SaveData();
		}
		return newBlockObj.GetComponent<TaskBlock>();
	}

	#endregion

	#region Save & Load

	//saves every 10 seconds
	private IEnumerator PeriodicDataSave() {
		while (true) {
			yield return new WaitForSeconds(10);
			SaveData();
		}
	}
	bool initializing = false;
	public void LoadData() {
		initializing = true;

		//TODO: if current data isn't empty, load it to some sort of backup

		//load channel
		if (!ChannelSaveLoad.LoadChannel(this, "test_save")) {
			//TODO: if save data doesn't exist, automatically try to download from online
		}
		initializing = false;
	}
	//NOTE: should be called whenever some sort of data has been changed (drag, input exit, etc)
	// and should periodically be called as well
	public void SaveData() {
		//don't save "changes" when loading in the data
		if (initializing) return;

		ChannelSaveLoad.SaveChannel(blocks, "test_save");
		Debug.Log("saved data");
	}
	public void TryClearData() {
		deleteAllBlocksScreen.gameObject.SetActive(true);
	}
	public void CancelClearData() {
		deleteAllBlocksScreen.gameObject.SetActive(false);
	}
	//NOTE: deletes all current data! MAKE SURE TO DOUBLE CHECK
	public void ClearData() {
		foreach (TaskBlock b in new List<TaskBlock>(blocks)) {
			b.DeleteBlock(true);
		}
		blocks.Clear();
		SaveData();
		deleteAllBlocksScreen.gameObject.SetActive(false);
	}
	public void TryUploadData() {
		//TODO: calls UI to verify user wants to update online data
	}
	public void TryDownloadData() {
		//TODO: confirms with user whether they want to override current data
		//initialization skips this and downloads directly if local data doesn't exist
	}

	#endregion

	public void SetShadow(Vector2 sizeDelta, float heightOffset, float leftOffset, float rightOffset) {
		if (!blockShadow.gameObject.activeInHierarchy)
			blockShadow.gameObject.SetActive(true);

		blockShadow.anchoredPosition = new Vector2(0, heightOffset);
		blockShadow.sizeDelta = sizeDelta;

		//left offset
		blockShadow.offsetMin = new Vector2(leftOffset, blockShadow.offsetMin.y);
		blockShadow.offsetMax = new Vector2(rightOffset, blockShadow.offsetMax.y);
	}
	public bool GetShadowIsActive() {
		return blockShadow.gameObject.activeInHierarchy;
	}
	public void RecalculateBlocks(bool recheckInputs = false) {
		//re-index all blocks' heights
		float totalContentHeight = -BLOCK_SPACING + BLOCK_TOP_PADDING;
		foreach (TaskBlock t in blocks) {
			float heightOffset = -(totalContentHeight + BLOCK_SPACING);
			if (recheckInputs) {
				t.UpdateAllInputChildren();
			}
			t.RecalculateBlock(heightOffset);
			if (t.GetIsDragged()) {
				SetShadow(t.GetComponent<RectTransform>().sizeDelta, heightOffset,
					(t.GetNestingIndex() + 1f) * TaskBlock.NESTING_WIDTH, t.GetTargetOffsetMax().x);
			}
			totalContentHeight += t.GetHeight() + BLOCK_SPACING;
		}
		scrollViewContent.sizeDelta = new Vector2(scrollViewContent.sizeDelta.x,
			totalContentHeight + BLOCK_SPACING + BLOCK_TOP_PADDING);
	}
	//called every frame by a dragged block to determine where the block can go
	//returns the first block [UNDER] the position of the mouse and the parent (default null)
	public KeyValuePair<TaskBlock, TaskBlock> GetBlockAtLocation(Vector2 blockPosition) {
		TaskBlock previousBlock = null;
		foreach (TaskBlock b in blocks) {
			if (b.GetIsDragged()) { continue; }
			if (b.transform.position.y < blockPosition.y + 10) {
				//look through previous block's children
				if (previousBlock != null) {
					KeyValuePair<TaskBlock, TaskBlock> blocksToReturn = previousBlock.GetBlockAtPosition(blockPosition);
					if (blocksToReturn.Key != null || blocksToReturn.Value != null) return blocksToReturn;
				}
				//no parent exists
				return new(b, null);
			}
			previousBlock = b;
		}
		//edge case: last block
		if (previousBlock != null) {
			KeyValuePair<TaskBlock, TaskBlock> blocksToReturn = previousBlock.GetBlockAtPosition(blockPosition);
			if (blocksToReturn.Key != null || blocksToReturn.Value != null) return blocksToReturn;
		}
		return new(null, null);
	}
	//rearranges blocks so that the movedBlock is now above the block titled aboveThis;
	//if aboveThis is null movedBlock is the last block
	public void RearrangeBlocks(TaskBlock movedBlock, TaskBlock aboveThis, TaskBlock parentBlock = null) {
		//if a parent for this exists, make sure to remove it from that parent's list of children
		movedBlock.DetachFromParent();

		if (parentBlock != null) {
			//Debug.Log($"set parent of movedBlock={movedBlock.GetBlockID()} to parentBlock={parentBlock.GetBlockID()}");
			if (parentBlock.GetChildrenList().Contains(movedBlock))
				parentBlock.GetChildrenList().Remove(movedBlock);

			//push to end of parent's children
			if (aboveThis == null) {
				parentBlock.GetChildrenList().Add(movedBlock);
			} else {
				parentBlock.GetChildrenList().Insert(
					parentBlock.GetChildrenList().IndexOf(aboveThis), movedBlock);
			}
			movedBlock.SetParent(parentBlock);
		} else {
			//Debug.Log($"Nest index: {movedBlock.GetNestingIndex()}");
			if (blocks.Contains(movedBlock))
				blocks.Remove(movedBlock);

			//push to end
			if (aboveThis == null) {
				blocks.Add(movedBlock);
			} else {
				blocks.Insert(blocks.IndexOf(aboveThis), movedBlock);
			}
			movedBlock.SetParent(null);
		}
		movedBlock.RecalculateIndent();
		movedBlock.UpdateAllInputChildren();
		foreach (TaskBlock b in movedBlock.GetChildrenList()) {
			b.RecalculateIndent();
		}
		RecalculateBlocks();

		//save changes
		SaveData();
	}

	public void DisableShadow() {
		if (blockShadow.gameObject.activeInHierarchy)
			blockShadow.gameObject.SetActive(false);
	}
	public void SetScrollViewDraggable(bool draggable) {
		scrollViewParent.vertical = draggable;
	}
	private void Update() {
	}
	private void Awake() {
		instance = this;

		LoadData();
		StartCoroutine(PeriodicDataSave());

		Application.targetFrameRate = 60;
	}
}
