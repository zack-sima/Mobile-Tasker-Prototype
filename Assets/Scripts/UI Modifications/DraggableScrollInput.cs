using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

//GPT-4 generated
[RequireComponent(typeof(TMP_InputField))]
public class DraggableScrollInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
	private ScrollRect scrollRect;

	public void OnBeginDrag(PointerEventData eventData) {
		if (scrollRect != null && !GetComponent<TMP_InputField>().isFocused) {
			// Forward the OnBeginDrag event to the ScrollRect
			scrollRect.OnBeginDrag(eventData);
		}
	}

	public void OnDrag(PointerEventData eventData) {
		if (scrollRect != null && !GetComponent<TMP_InputField>().isFocused) {
			// Forward the OnDrag event to the ScrollRect
			scrollRect.OnDrag(eventData);
		}
	}

	public void OnEndDrag(PointerEventData eventData) {
		if (scrollRect != null && !GetComponent<TMP_InputField>().isFocused) {
			// Forward the OnEndDrag event to the ScrollRect
			scrollRect.OnEndDrag(eventData);
		}
	}
	void Start() {
		scrollRect = BlockMaster.instance.GetScrollRect();
	}
}
