using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

//GPT-4 generated (+ some modifications)
[AddComponentMenu("UI/Custom Input Field", 32)]
public class CustomInputField : TMP_InputField {
	//NOTE: can only be populated in debug mode!
	[SerializeField] private Image dimImage;
	[SerializeField] private Color dimColor;

	// Override the OnPointerDown method to do nothing
	public override void OnPointerDown(PointerEventData eventData) {
		// Do not call base.OnPointerDown(eventData) to prevent focus on mouse down
		if (dimImage != null)
			dimImage.color = dimColor;

		if (isFocused) {
			base.OnPointerDown(eventData);
		}
	}

	// Implement the OnPointerUp method to set focus
	public override void OnPointerUp(PointerEventData eventData) {
		// Check if the pointer is within the bounds of the InputField
		if (IsPointerWithinBounds(eventData) && !isFocused) {
			//base.OnPointerDown(eventData); // Call the base OnPointerDown here instead
			SetFocus();
		}
		if (dimImage != null)
			dimImage.color = Color.white;
	}
	// Method to set focus to this input field
	private void SetFocus() {
		if (EventSystem.current != null) {
			EventSystem.current.SetSelectedGameObject(
				gameObject, new BaseEventData(EventSystem.current));
		}
	}
	public override void OnUpdateSelected(BaseEventData eventData) {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			// Add your logic here for what should happen when Escape is pressed
			// For example, clear focus:
			EventSystem.current.SetSelectedGameObject(null);

			// Optionally, do additional things like closing a menu
		} else {
			// Call the base class method to handle normal input field updates
			base.OnUpdateSelected(eventData);
		}
	}
	// Check if the pointer is within the bounds of the InputField
	private bool IsPointerWithinBounds(PointerEventData eventData) {
		RectTransform rectTransform = GetComponent<RectTransform>();
		return RectTransformUtility.RectangleContainsScreenPoint(
			rectTransform, eventData.position, eventData.pressEventCamera);
	}
}
