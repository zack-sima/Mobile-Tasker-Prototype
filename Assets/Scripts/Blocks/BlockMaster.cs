using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the dragging & arrangement of blocks, and the scroll view on the Main Canvas.
/// </summary>

public class BlockMaster : MonoBehaviour {
	public static BlockMaster instance;

	#region Prefabs

	[SerializeField] private GameObject blockPrefab;
	[SerializeField] private GameObject backupRowPrefab;

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

	[SerializeField] private RectTransform backupsScreen;
	[SerializeField] private RectTransform backupsScrollViewContent;

	[SerializeField] private TMP_InputField titleInputField;
	public string GetTitle() { return titleInputField.text; }
	public void SetTitle(string t) { titleInputField.text = t; }

	[SerializeField] private DataDownload downloader;

	#endregion

	#region Members

	//keep track of all the parent blocks (each block has a list of its children)
	private List<TaskBlock> blocks = new();
	public List<TaskBlock> GetBlocks() { return blocks; }

	//keeps track of up to 100 undos and redos
	private LinkedList<string> undoStack = new();
	private LinkedList<string> redoStack = new();

	//whether to show delete button, set clock button, etc...
	private bool showOptions = false;
	public bool GetShowOptions() { return showOptions; }

	//don't track undo/redo or save data when this is marked on
	bool initializing = false;

	//backups rows
	private List<BackupRow> backupRows = new();

	//channel ID for loading channel
	private string channelId = "test_save";

	//check before allowing uploading
	private bool canUpload = false;

	//set this to false when mouse just pressed, and true whenever mouse is pressed but dragged
	private bool mouseScrolled = false;
	public bool GetMouseScrolled() { return mouseScrolled; }

	private Vector2 mouseDownPosition = Vector2.zero;

	#endregion

	#region Button Callbacks

	public void ExitChannel() {
		SaveData(autoSave: true);
		CreateLastBackup();
		SceneManager.LoadScene(0);
	}
	public void TryUndo() {
		UndoStep();
	}
	public void TryRedo() {
		RedoStep();
	}
	public void ShowNormalBar() {
		optionsMenuBar.gameObject.SetActive(false);
		normalMenuBar.gameObject.SetActive(true);
	}
	public void ShowOptionsBar() {
		optionsMenuBar.gameObject.SetActive(true);
		normalMenuBar.gameObject.SetActive(false);
	}
	public void ShowBackups() {
		backupsScreen.gameObject.SetActive(true);
		RenderBackups();
	}
	public void HideBackups() {
		backupsScreen.gameObject.SetActive(false);
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

	#region Undo & Redo

	private void UndoStep() {
		if (undoStack.Count <= 1) {
			Debug.Log("No more undos left!");
			return;
		}

		initializing = true;

		redoStack.AddLast(undoStack.Last.Value);

		undoStack.RemoveLast();

		if (undoStack.Count > 0) {
			ChannelSaveLoad.LoadChannelWithString(this, undoStack.Last.Value);
		}

		initializing = false;

		//don't trigger undo stack adding
		SaveData(autoSave: true);
		TryUploadData();
	}
	private void RedoStep() {
		if (redoStack.Count == 0) {
			Debug.Log("No more redos left!");
			return;
		}

		initializing = true;

		ChannelSaveLoad.LoadChannelWithString(this, redoStack.Last.Value);
		undoStack.AddLast(redoStack.Last.Value);
		redoStack.RemoveLast();

		initializing = false;

		//don't trigger undo stack adding
		SaveData(autoSave: true);
		TryUploadData();
	}
	//only saves the last 100 moves to prevent memory issues
	private void PopFullStack<T>(LinkedList<T> stack) {
		while (stack.Count > 100) stack.RemoveFirst();
	}

	#endregion

	#region Backups

	//re-renders the backup list for the scroll view
	public void RenderBackups() {
		//delete existing backup rows to clean up
		foreach (BackupRow r in backupRows) {
			Destroy(r.gameObject);
		}
		backupRows.Clear();

		//create new rows in backup scroll view
		ChannelBackup.BackupData data = ChannelBackup.GetBackups(channelId);
		foreach (KeyValuePair<string, ChannelBackup.SingleBackup> kv in data.backups) {
			BackupRow r = Instantiate(backupRowPrefab,
				backupsScrollViewContent).GetComponent<BackupRow>();
			r.SetBackupId(kv.Key);
			backupRows.Add(r);
		}
	}
	public void CreateLastBackup() {
		ChannelBackup.AddBackup(ChannelSaveLoad.CreateChannelString(this), false, true, channelId);
	}
	public void CreateBackup() {
		//creates a backup of the current timestamp
		ChannelBackup.AddBackup(ChannelSaveLoad.CreateChannelString(this), false, false, channelId);
		RenderBackups();
	}
	public void LoadBackup(string backupId) {
		initializing = true;
		ChannelBackup.LoadBackup(this, backupId, channelId);
		initializing = false;

		HideBackups();
	}
	public void DeleteBackup(string backupId) {
		//deletes a backup
		ChannelBackup.RemoveBackup(backupId, channelId);

		//re-render backups
		RenderBackups();
	}

	#endregion

	#region Save & Load

	//saves every 10 seconds
	private IEnumerator PeriodicDataSave() {
		while (true) {
			yield return new WaitForSeconds(10);
			SaveData(autoSave: true);
			ChannelBackup.AddBackup(ChannelSaveLoad.CreateChannelString(this), true, false, channelId);
			RenderBackups();
		}
	}

	public void LoadData() {
		initializing = true;

		//load channel
		ChannelSaveLoad.LoadChannel(this, channelId);

		//automatically try to download from online regardless
		TryDownloadData();

		ChannelLoaded();
		initializing = false;
	}
	public void ChannelLoaded() {
		StartCoroutine(ChannelLoadedCoroutine());
	}
	private IEnumerator ChannelLoadedCoroutine() {
		for (int i = 0; i < 2; i++) yield return new WaitForEndOfFrame();
		RecalculateBlocks(true);
		SaveData(false);
	}
	//NOTE: should be called whenever some sort of data has been changed (drag, input exit, etc)
	// and should periodically be called as well
	public void SaveData(bool autoSave) {
		//don't save "changes" when loading in the data
		if (initializing) return;

		ChannelSaveLoad.SaveChannel(this, channelId);

		if (!autoSave) {
			Debug.Log("made save!");

			string saveStr = ChannelSaveLoad.CreateChannelString(this);
			undoStack.AddLast(saveStr);
			PopFullStack(undoStack);

			redoStack.Clear();

			TryUploadData();
		}
	}
	public void SaveData() {
		SaveData(false);
	}
	public void TryClearData() {
		deleteAllBlocksScreen.gameObject.SetActive(true);
	}
	public void CancelClearData() {
		deleteAllBlocksScreen.gameObject.SetActive(false);
	}
	//NOTE: deletes all current data! MAKE SURE TO DOUBLE CHECK
	public void ClearData(bool withoutSave) {
		foreach (TaskBlock b in new List<TaskBlock>(blocks)) {
			b.DeleteBlock(true);
		}
		blocks.Clear();

		deleteAllBlocksScreen.gameObject.SetActive(false);

		if (!initializing && !withoutSave) {
			SaveData();
			ShowNormalBar();
		}
	}
	public void ClearData() { ClearData(false); }
	public void TryUploadData() {
		if (!canUpload || DataUpload.instance == null) return;
		DataUpload.instance.SendData(channelId, ChannelSaveLoad.CreateChannelString(this));
	}
	public void TryDownloadData() {
		//initialization skips this and downloads directly if local data doesn't exist
		downloader.GetData(channelId);
	}
	public void DataDownloaded() {
		canUpload = true;
	}

	#endregion

	#region Blocks Logic

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
	public void RearrangeBlocks(TaskBlock movedBlock, TaskBlock aboveThis, TaskBlock parentBlock = null, bool finishedRearranging = false) {
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

		//save changes (if not dragging anymore)
		if (finishedRearranging)
			SaveData();
	}

	public void DisableShadow() {
		if (blockShadow.gameObject.activeInHierarchy)
			blockShadow.gameObject.SetActive(false);
	}
	public void SetScrollViewDraggable(bool draggable) {
		scrollViewParent.vertical = draggable;
	}

	#endregion

	#region Unity Functions

	private void Update() {
		if (Input.GetMouseButtonDown(0)) {
			mouseScrolled = false;
			mouseDownPosition = Input.mousePosition;
		}
		if (Input.GetMouseButton(0)) {
			if (Vector2.Distance((Vector2)Input.mousePosition, mouseDownPosition) > 10) {
				mouseScrolled = true;
			}
		}
	}
	private void Awake() {
		instance = this;
	}
	private void Start() {
		channelId = PlayerPrefs.GetString("current_channel");
		if (channelId == "") channelId = "test_save";

		LoadData();

		StartCoroutine(PeriodicDataSave());

		Application.targetFrameRate = 60;
	}

	#endregion
}
