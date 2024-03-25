using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class ChannelBackup {
	[System.Serializable]
	public class BackupData {
		public Dictionary<string, SingleBackup> backups;
	}
	[System.Serializable]
	public class SingleBackup {
		public string channelData;
		public bool isAutoSave;
		public long timestamp;
	}
	public static BackupData GetBackups(string channelId) {
		try {
			string s = PlayerPrefs.GetString($"channel_backups_{channelId}");
			BackupData d = (BackupData)MyJsonUtility.FromJson(typeof(BackupData), s);
			d.backups ??= new();

			List<KeyValuePair<string, SingleBackup>> sortedBackups = new(d.backups);
			sortedBackups.Sort((pair1, pair2) => pair2.Value.timestamp.CompareTo(pair1.Value.timestamp));
			d.backups = new(sortedBackups);

			return d;
		} catch (System.Exception e) {
			Debug.LogWarning($"Backup loading failed: {e}");
			return new() { backups = new() };
		}
	}
	public static void LoadBackup(BlockMaster master, string backupId, string channelId) {
		try {
			string s = PlayerPrefs.GetString($"channel_backups_{channelId}");
			BackupData d = (BackupData)MyJsonUtility.FromJson(typeof(BackupData), s);
			d.backups ??= new();

			if (d.backups.ContainsKey(backupId)) {
				ChannelSaveLoad.LoadChannelWithString(master, d.backups[backupId].channelData);
			}
		} catch (System.Exception e) {
			Debug.LogWarning($"Backup loading failed: {e}");
			return;
		}
	}
	public static void RemoveBackup(string backupId, string channelId) {
		BackupData d;
		try {
			string s = PlayerPrefs.GetString($"channel_backups_{channelId}");
			d = (BackupData)MyJsonUtility.FromJson(typeof(BackupData), s);
			d.backups ??= new();
			if (d.backups.ContainsKey(backupId)) {
				d.backups.Remove(backupId);
			}
			PlayerPrefs.SetString($"channel_backups_{channelId}", MyJsonUtility.ToJson(typeof(BackupData), d));
		} catch {
			return;
		}
	}
	public static void AddBackup(string data, bool isAutoSave, string channelId) {
		BackupData d;
		try {
			string s = PlayerPrefs.GetString($"channel_backups_{channelId}");
			d = (BackupData)MyJsonUtility.FromJson(typeof(BackupData), s);
		} catch {
			d = new();
		}
		string saveName = isAutoSave ? "[Autosave]" : "[Backup]";
		string backupId = $"{saveName} {System.DateTime.Now}";

		d.backups ??= new();
		d.backups.TryAdd(backupId, new() {
			channelData = data,
			isAutoSave = isAutoSave,
			timestamp = System.DateTime.Now.ToBinary() / 10_000_000 //seconds
		});

		//cleanup auto backups; last minute all backups kept, last ten minuts one per minute, etc
		long currTime = System.DateTime.Now.ToBinary() / 10_000_000; //seconds
		long lastTenMinuteBackupTimestamp = currTime,
			lastHourBackupTimestamp = currTime,
			lastDayBackupTimestamp = currTime,
			lastWeekBackupTimestamp = currTime;

		List<KeyValuePair<string, SingleBackup>> sortedBackups = new(d.backups);
		sortedBackups.Sort((pair1, pair2) => pair2.Value.timestamp.CompareTo(pair1.Value.timestamp));

		foreach (KeyValuePair<string, SingleBackup> backup in sortedBackups) {
			if (!backup.Value.isAutoSave) continue; //player saves are permanent
			if (backup.Value.timestamp > currTime - 125) continue; //backup all recent

			//last 10 minutes, 1 minute per backup
			if (backup.Value.timestamp > currTime - 600 &&
				backup.Value.timestamp < lastTenMinuteBackupTimestamp - 60) {
				lastTenMinuteBackupTimestamp = backup.Value.timestamp;
				continue;
			}
			//last hour, 10 minutes per backup
			if (backup.Value.timestamp > currTime - 3600 &&
				backup.Value.timestamp < lastHourBackupTimestamp - 600) {
				lastHourBackupTimestamp = backup.Value.timestamp;
				continue;
			}
			//last day, 1 hour per backup
			if (backup.Value.timestamp > currTime - 3600 * 24 &&
				backup.Value.timestamp < lastDayBackupTimestamp - 3600) {
				lastDayBackupTimestamp = backup.Value.timestamp;
				continue;
			}
			//last week, 1 day per backup
			if (backup.Value.timestamp > currTime - 3600 * 24 * 7 &&
				backup.Value.timestamp < lastWeekBackupTimestamp - 3600 * 24) {
				lastWeekBackupTimestamp = backup.Value.timestamp;
				continue;
			}
			//otherwise remove backup
			d.backups.Remove(backup.Key);

			//Debug.Log($"{currTime}, {backup.Value.timestamp}");
			//Debug.Log($"Removed backup: {backup.Key}, time difference={currTime - backup.Value.timestamp}");
		}
		PlayerPrefs.SetString($"channel_backups_{channelId}", MyJsonUtility.ToJson(typeof(BackupData), d));
	}
}