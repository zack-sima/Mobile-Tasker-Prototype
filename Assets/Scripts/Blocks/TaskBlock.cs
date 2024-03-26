using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class TaskBlock : MonoBehaviour {
	//default height of block when created
	public const float DEFAULT_HEIGHT = 100f;
	public const float NESTING_WIDTH = 50f;

	#region Members

	//changed when anything modifies block
	private float height;
	public float GetHeight() { return height; }

	//how deep a block is nested
	private int nestingIndex;
	public int GetNestingIndex() { return nestingIndex; }

	//when dragged, ignore position of this when re-sorting block indices
	private bool isDragged = false;
	public bool GetIsDragged() { return isDragged; }
	public void SetIsDragged(bool d) { isDragged = d; }

	//position offset between object position and mouse position
	private Vector2 relativeDragPosition = Vector2.zero;
	public void SetRelativeDragPosition(Vector2 p) { relativeDragPosition = p; }

	//block nesting
	private List<TaskBlock> children = new();
	public List<TaskBlock> GetChildrenList() { return children; }

	private TaskBlock parent = null;
	public TaskBlock GetParent() { return parent; }

	//target indentation (ignore when dragging)
	Vector2 targetOffsetMin = Vector2.zero, targetOffsetMax = Vector2.zero;
	public Vector2 GetTargetOffsetMin() { return targetOffsetMin; }
	public Vector2 GetTargetOffsetMax() { return targetOffsetMax; }

	//if folded, don't render any children and turn folded icon to the side
	private bool isFolded = false;
	public bool GetIsFolded() { return isFolded; }
	public void SetIsFolded(bool f) { isFolded = f; UpdateFold(); }

	//if focus just changed (now not focused), re-adjust height (iOS cancel inputfield bug)
	private bool inputWasFocused = false;

	#endregion

	#region References

	[SerializeField] private TMP_InputField textInputField;
	public string GetText() { return textInputField.text; }
	public void SetText(string t) { textInputField.text = t; }

	[SerializeField] private Image bulletPointImage;
	[SerializeField] private Sprite bulletPointSprite, dragRowSprite;
	[SerializeField] private TMP_Text invisibleTextSizer; //used to determine block height
	[SerializeField] private Button foldButton;
	[SerializeField] private List<Button> optionsButtons; //hidden unless options is toggled on

	#endregion

	#region Dragging

	private void UpdateDragging() {
		transform.position = (Vector2)Input.mousePosition - relativeDragPosition;

		//set this block right above blocks.Key and inside parent (blocks.Value)
		KeyValuePair<TaskBlock, TaskBlock> blocks = BlockMaster.instance.GetBlockAtLocation(transform.position);

		BlockMaster.instance.RearrangeBlocks(this, blocks.Key, blocks.Value, finishedRearranging: Input.GetMouseButtonUp(0));

		//NOTE: for some reason, OnPointerUp doesn't work properly here
		if (Input.GetMouseButtonUp(0)) {
			isDragged = false;
			BlockMaster.instance.SetScrollViewDraggable(true);
			BlockMaster.instance.RecalculateBlocks();
			BlockMaster.instance.DisableShadow();
			StartCoroutine(WaitUpdateBlocks());
		}
	}
	private IEnumerator WaitUpdateBlocks() {
		for (int i = 0; i < 2; i++)
			yield return new WaitForEndOfFrame();
		BlockMaster.instance.RecalculateBlocks();
	}

	#endregion

	#region Callbacks

	public void TryDeleteBlock() {
		BlockMaster.instance.DeleteBlockCalled(this);
	}
	public void AddChildBlock() {
		TaskBlock newBlock = BlockMaster.instance.CreateBlock(recalculate: false);
		newBlock.SetParent(this);
		children.Insert(0, newBlock);
		BlockMaster.instance.RecalculateBlocks();
	}
	public void DeleteBlock(bool source = true) {
		//recursively deletes all children as well
		foreach (TaskBlock tb in new List<TaskBlock>(children)) {
			tb.DeleteBlock(false);
		}
		if (parent != null) {
			parent.children.Remove(this);
		} else {
			BlockMaster.instance.GetBlocks().Remove(this);
		}
		if (source) {
			BlockMaster.instance.RecalculateBlocks();
		}
		Destroy(gameObject);
	}
	public void ToggleShowOptions(bool optionsOn) {
		foreach (TaskBlock tb in children) {
			tb.ToggleShowOptions(optionsOn);
		}
		foreach (Button b in optionsButtons) {
			b.gameObject.SetActive(optionsOn);
		}
		bulletPointImage.sprite = optionsOn ? dragRowSprite : bulletPointSprite;
		foldButton.gameObject.SetActive(children.Count != 0 && !optionsOn);
	}
	public void ToggleFold() {
		isFolded = !isFolded;
		UpdateFold();
	}
	public void UpdateFold() {
		//call re-render on block master
		foldButton.transform.eulerAngles = new Vector3(0, 0, isFolded ? 90 : 0);
		BlockMaster.instance.RecalculateBlocks();
		BlockMaster.instance.SaveData();
	}

	//updates the inputfields of all children to be resized properly
	public void UpdateAllInputChildren() {
		foreach (TaskBlock tb in children) {
			tb.UpdateAllInputChildren();
		}
		OnInputFieldChanged(false);
		BlockMaster.instance.RecalculateBlocks();
	}
	public void OnInputFieldDeselected() {
		//save changes
		BlockMaster.instance.SaveData();
	}
	public void OnInputFieldChanged(bool recalculate = true, bool forceRecalculate = false) {
		StartCoroutine(WaitUpdateInputSize(recalculate, forceRecalculate));
	}
	private IEnumerator WaitUpdateInputSize(bool recalculate, bool forceRecalculate) {
		yield return new WaitForEndOfFrame();
		float oldHeight = height;
		RecalculateHeight();
		if (height != oldHeight || forceRecalculate) {
			RectTransform rt = GetComponent<RectTransform>();
			rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
			if (recalculate)
				BlockMaster.instance.RecalculateBlocks();
		}
	}

	#endregion

	//recursively tries to get children blocks' positions (and checks if position's X
	///  warrents a traversal into children depth) to find the block to return
	public KeyValuePair<TaskBlock, TaskBlock> GetBlockAtPosition(Vector2 position) {
		//set to equal-layer
		if (position.x < transform.position.x + 10 || isFolded) return new(null, null);

		TaskBlock previousBlock = null;

		//find first child with position y < position, then go to previous child and
		//  recursively call GetBlockAtPosition on its children
		foreach (TaskBlock b in children) {
			if (b.GetIsDragged()) continue;
			if (b.transform.position.y < position.y + 10) {
				//look through previous block's children
				if (previousBlock != null) {
					KeyValuePair<TaskBlock, TaskBlock> blocksToReturn = previousBlock.GetBlockAtPosition(position);
					if (blocksToReturn.Key != null || blocksToReturn.Value != null) return blocksToReturn;
				}
				//no child exists on previous
				return new(b, this);
			}
			previousBlock = b;
		}
		//edge case: last block
		if (previousBlock != null) {
			KeyValuePair<TaskBlock, TaskBlock> blocksToReturn = previousBlock.GetBlockAtPosition(position);
			if (blocksToReturn.Key != null || blocksToReturn.Value != null) return blocksToReturn;
		}
		return new(null, this);
	}
	public void DetachFromParent() {
		if (parent != null) {
			//remove self from parent
			if (parent.GetChildrenList().Contains(this))
				parent.GetChildrenList().Remove(this);
		} else {
			if (BlockMaster.instance.GetBlocks().Contains(this))
				BlockMaster.instance.GetBlocks().Remove(this);
		}
	}
	public void SetParent(TaskBlock parent) {
		this.parent = parent;
		RecalculateIndent();
	}
	public void RecalculateIndent() {
		if (parent == null) {
			nestingIndex = 0;
		} else {
			nestingIndex = parent.GetNestingIndex() + 1;
		}

		targetOffsetMin = new Vector2((nestingIndex + 1f) * NESTING_WIDTH, targetOffsetMin.y);

		RectTransform rt = GetComponent<RectTransform>();

		if (!isDragged) {
			rt.offsetMin = new Vector2(targetOffsetMin.x, rt.offsetMin.y);
			rt.offsetMax = new Vector2(targetOffsetMax.x, rt.offsetMax.y);
		}
	}
	public void RecalculateBlock(float heightOffset, bool isParentFolded = false) {
		//if parent is folded set scale to 0 (hides rendering, etc)
		transform.localScale = new Vector2(isParentFolded ? 0 : 1, 1);

		//disable fold button if no children exist
		foldButton.gameObject.SetActive(children.Count != 0 && !BlockMaster.instance.GetShowOptions());

		RecalculateHeight();

		foreach (TaskBlock b in children) {
			//either parent or self being folded means next layer is folded
			b.RecalculateBlock(heightOffset - height - BlockMaster.BLOCK_SPACING,
				isParentFolded || isFolded);
			if (!isFolded)
				height += b.GetHeight() + BlockMaster.BLOCK_SPACING;
		}
		if (!isDragged) {
			GetComponent<RectTransform>().anchoredPosition = new Vector2(
				GetComponent<RectTransform>().anchoredPosition.x, heightOffset);
		} else {
			BlockMaster.instance.SetShadow(GetComponent<RectTransform>().sizeDelta, heightOffset,
				(nestingIndex + 1f) * NESTING_WIDTH, GetTargetOffsetMax().x);
		}
		RecalculateIndent();
	}
	private void RecalculateHeight() {
		//calculate new height of input
		invisibleTextSizer.text = textInputField.text;
		if (invisibleTextSizer.text.Length > 0 && invisibleTextSizer.text[^1] == '\n') {
			invisibleTextSizer.text += "a"; //auto-resize ignores invisible newline
		}
		height = Mathf.Max(DEFAULT_HEIGHT,
			invisibleTextSizer.GetComponent<RectTransform>().sizeDelta.y + 35f);
	}
	private void Awake() {
		height = DEFAULT_HEIGHT;
		nestingIndex = 0;
		targetOffsetMin = GetComponent<RectTransform>().offsetMin;
		targetOffsetMax = GetComponent<RectTransform>().offsetMax;
	}
	private void Start() {
		ToggleShowOptions(BlockMaster.instance.GetShowOptions());
	}
	private void Update() {
		if (isDragged) {
			UpdateDragging();
		}
		if (textInputField.isFocused || inputWasFocused) {
			OnInputFieldChanged(forceRecalculate: inputWasFocused && !textInputField.isFocused);
		}
		inputWasFocused = textInputField.isFocused;
	}
}
