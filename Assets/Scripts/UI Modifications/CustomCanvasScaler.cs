using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class CustomCanvasScaler : MonoBehaviour {
	public static CustomCanvasScaler instance;

	private void Awake() {
		instance = this;
		ScaleCanvas();
	}
	public static void ScaleCanvas() {
		if (instance == null) return;
		instance.GetComponent<CanvasScaler>().referenceResolution =
			new Vector2(1080f - PlayerPrefs.GetFloat("ui_scale") * 100f, 1920f);
	}
}
