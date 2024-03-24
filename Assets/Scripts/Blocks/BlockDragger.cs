using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BlockDragger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
	[SerializeField] private TaskBlock taskBlock;

	public void OnPointerDown(PointerEventData eventData) {
		taskBlock.SetIsDragged(true);
		taskBlock.SetRelativeDragPosition(Input.mousePosition - taskBlock.transform.position);
		taskBlock.GetComponent<RectTransform>().SetAsLastSibling();
		BlockMaster.instance.SetScrollViewDraggable(false);
	}
	public void OnPointerUp(PointerEventData eventData) { }
}
