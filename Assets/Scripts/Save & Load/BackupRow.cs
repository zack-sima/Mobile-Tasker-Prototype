using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BackupRow : MonoBehaviour {
	//references
	[SerializeField] private TMP_Text backupIdDisplay;

	//NOTE: set this via script when spawning!
	[HideInInspector] public string backupId;

	public void SetBackupId(string backupId) {
		this.backupId = backupId;
		backupId = backupId.Replace("[Backup]", "<color=#33FF33>[Backup]</color>");
		backupIdDisplay.text = backupId;
	}
	public void LoadBackup() {
		//load backup and stop popup
		BlockMaster.instance.LoadBackup(backupId);
	}
	public void DeleteBackup() {
		//delete this backup and update backup list again
		BlockMaster.instance.DeleteBackup(backupId);
	}
}