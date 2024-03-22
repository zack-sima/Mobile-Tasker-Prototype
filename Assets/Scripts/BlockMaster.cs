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

	#region References & Getters

	[SerializeField] private Canvas mainCanvas;
	public Canvas GetMainCanvas() { return mainCanvas; }
	public float GetCanvasScaleFactor() { return mainCanvas.scaleFactor; }

	[SerializeField] private ScrollRect scrollViewParent;
	[SerializeField] private RectTransform scrollViewContent;

	//shows where the block would go with indents when a block is dragged
	[SerializeField] private RectTransform blockShadow;

	#endregion

	#region Members

	//keep track of all the parent blocks (each block has a list of its children)
	private List<TaskBlock> blocks = new();
	public List<TaskBlock> GetBlocks() { return blocks; }

	#endregion

	//called by button; creates empty block in FRONT
	public void CreateBlock() {
		GameObject newBlockObj = Instantiate(blockPrefab, scrollViewContent);

		//TODO: temp id set
		newBlockObj.GetComponent<TaskBlock>().SetBlockID(blocks.Count);

		blocks.Insert(0, newBlockObj.GetComponent<TaskBlock>());
		RecalculateBlocks();
	}
	public void SetShadow(Vector2 sizeDelta, float heightOffset, float leftOffset, float rightOffset) {
		if (!blockShadow.gameObject.activeInHierarchy)
			blockShadow.gameObject.SetActive(true);

		blockShadow.anchoredPosition = new Vector2(0, heightOffset);
		blockShadow.sizeDelta = sizeDelta;

		//left offset
		blockShadow.offsetMin = new Vector2(leftOffset, blockShadow.offsetMin.y);
		blockShadow.offsetMax = new Vector2(rightOffset, blockShadow.offsetMax.y);
	}
	public void RecalculateBlocks() {
		//re-index all blocks' heights
		float totalContentHeight = TaskBlock.DEFAULT_HEIGHT / 2f - BLOCK_SPACING + BLOCK_TOP_PADDING;
		foreach (TaskBlock t in blocks) {
			float heightOffset = -(totalContentHeight + BLOCK_SPACING);
			t.RecalculateBlock(heightOffset);
			if (t.GetIsDragged()) {
				SetShadow(t.GetComponent<RectTransform>().sizeDelta, heightOffset,
					(t.GetNestingIndex() + 1f) * TaskBlock.NESTING_WIDTH, t.GetTargetOffsetMax().x);
			}
			totalContentHeight += t.GetHeight() + BLOCK_SPACING;
		}
		scrollViewContent.sizeDelta = new Vector2(scrollViewContent.sizeDelta.x, totalContentHeight);
	}
	//called every frame by a dragged block to determine where the block can go
	//returns the first block [UNDER] the position of the mouse and the parent (default null)
	public KeyValuePair<TaskBlock, TaskBlock> GetBlockAtLocation(Vector2 blockPosition) {
		//TODO: find children blocks; if dragged to left of center screen, don't nest in children blocks

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
		foreach (TaskBlock b in movedBlock.GetChildrenList()) {
			b.RecalculateIndent();
		}
		RecalculateBlocks();
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
	}
}
