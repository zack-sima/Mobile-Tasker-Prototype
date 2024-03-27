using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsMaster : MonoBehaviour {
	[SerializeField] private Slider uiSizeSlider;

	public void OnSliderChanged() {
		PlayerPrefs.SetFloat("ui_scale", uiSizeSlider.value);
		CustomCanvasScaler.ScaleCanvas();
	}
	public void GoToMenu() {
		SceneManager.LoadScene(0);
	}
	private void Awake() {
		if (Application.isMobilePlatform) {
			uiSizeSlider.minValue = -1;
		}
		uiSizeSlider.value = PlayerPrefs.GetFloat("ui_scale");
	}
}
