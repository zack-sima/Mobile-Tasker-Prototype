using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TaskBlock : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
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

	//position offset between object position and mouse position
	private Vector2 relativeDragPosition = Vector2.zero;

	//TODO: block nesting
	private List<TaskBlock> children = new();
	public List<TaskBlock> GetChildrenList() { return children; }

	private TaskBlock parent = null;
	public TaskBlock GetParent() { return parent; }

	//target indentation (ignore when dragging)
	Vector2 targetOffsetMin = Vector2.zero, targetOffsetMax = Vector2.zero;
	public Vector2 GetTargetOffsetMin() { return targetOffsetMin; }
	public Vector2 GetTargetOffsetMax() { return targetOffsetMax; }

	#endregion

	#region Dragging

	public void OnPointerDown(PointerEventData eventData) {
		isDragged = true;
		relativeDragPosition = Input.mousePosition - transform.position;
		GetComponent<RectTransform>().SetAsLastSibling();
		BlockMaster.instance.SetScrollViewDraggable(false);
	}
	public void OnPointerUp(PointerEventData eventData) { }
	private void UpdateDragging() {
		transform.position = (Vector2)Input.mousePosition - relativeDragPosition;

		//set this block right above blocks.Key and inside parent (blocks.Value)
		KeyValuePair<TaskBlock, TaskBlock> blocks = BlockMaster.instance.GetBlockAtLocation(transform.position);
		//string k = blocks.Key != null ? blocks.Key.GetBlockID() : "none";
		//string v = blocks.Value != null ? blocks.Value.GetBlockID() : "none";
		//Debug.Log($"found block: {k}, {v}");
		BlockMaster.instance.RearrangeBlocks(this, blocks.Key, blocks.Value);

		//NOTE: for some reason, OnPointerUp doesn't work properly here
		if (Input.GetMouseButtonUp(0)) {
			isDragged = false;
			BlockMaster.instance.SetScrollViewDraggable(true);
			BlockMaster.instance.RecalculateBlocks();
			BlockMaster.instance.DisableShadow();
		}
	}

	#endregion

	//recursively tries to get children blocks' positions (and checks if position's X
	///  warrents a traversal into children depth) to find the block to return
	public KeyValuePair<TaskBlock, TaskBlock> GetBlockAtPosition(Vector2 position) {
		if (position.x < transform.position.x + 10) return new(null, null);

		//find first child with position y < position, then go to previous child and
		//  recursively call GetBlockAtPosition on its children
		TaskBlock previousBlock = null;
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
	//TODO: temporary indexer for checking dragging, etc
	public string GetBlockID() {
		return transform.GetChild(0).GetComponent<TMP_Text>().text;
	}
	//TODO: temporary indexer for checking dragging, etc
	public void SetBlockID(int id) {
		transform.GetChild(0).GetComponent<TMP_Text>().text = $"Block {id}";
	}
	public void RecalculateBlock(float heightOffset) {
		height = DEFAULT_HEIGHT;

		foreach (TaskBlock b in children) {
			//Debug.Log($"Nest index: {b.GetNestingIndex()}");
			b.RecalculateBlock(heightOffset - height - BlockMaster.BLOCK_SPACING);
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
	private void Awake() {
		height = DEFAULT_HEIGHT;
		nestingIndex = 0;
		targetOffsetMin = GetComponent<RectTransform>().offsetMin;
		targetOffsetMax = GetComponent<RectTransform>().offsetMax;
	}
	private void Update() {
		if (isDragged) {
			UpdateDragging();
		}
	}
}
