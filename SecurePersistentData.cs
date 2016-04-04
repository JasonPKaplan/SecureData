using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using LitJson;

public class SecurePersistentData
{
	private const string NO_FILENAME = "NO_FILENAME";
	private const string SUPER_SECRET = "supersecret";
	private const string FILENAME = "secure";

	private static SecurePersistentData _Instance;
	private Dictionary<string, object> _StoreData;
	
	public static SecurePersistentData Instance
	{
		get
		{
			if(_Instance == null)
			{
				_Instance = new SecurePersistentData();
				if (File.Exists (Filename()))
				{
					Load ();
				}
			}
			return _Instance;
		}
	}

	public SecurePersistentData()
	{
		#if UNITY_EDITOR
		if(!Directory.Exists(Application.streamingAssetsPath))
		{
			Directory.CreateDirectory(Application.streamingAssetsPath);
		}
		#endif
		if (!File.Exists (Filename()))
		{
			_StoreData = new Dictionary<string, object>();
		}
	}

	public static string Filename()
	{
		#if UNITY_EDITOR
		return Path.Combine(Application.streamingAssetsPath, FILENAME);
		#endif
		return Path.Combine(Application.persistentDataPath, FILENAME);
	}

	/// <summary>
	/// Removes key from dictionary. Use with caution
	/// </summary>
	/// <param name="key">Store key.</param>
	public static void Reset(string key, bool save = false)
	{
		if(Instance._StoreData.ContainsKey(key))
		{
			Instance._StoreData.Remove (key);

		}

		if(save)
		{
			Save ();
		}
	}

	/// <summary>
	/// Set the specified key and val.
	/// </summary>
	/// <param name="key">Store key.</param>
	/// <param name="val">Store value.</param>
	public static void Set(string key, object val, bool save = false)
	{
		if(Instance._StoreData.ContainsKey(key))
		{
			Instance._StoreData[key] = val;

		}
		else
		{
			Instance._StoreData.Add(key, val);
		}

		if(save)
		{
			Save ();
		}
	}

	/// <summary>
	/// Get the value for the specified key.
	/// </summary>
	/// <param name="key">Store key.</param>
	public static object Get(string key, object ret = null)
	{
		if(Instance._StoreData.ContainsKey(key))
		{
			return Instance._StoreData[key];
		}
		return ret;
	}

	/// <summary>
	/// Load store data.
	/// </summary>
	public static void Load()
	{
		string streamString = string.Empty;

		using (Stream stream = new FileStream(Filename(), FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			System.Runtime.Serialization.IFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter ();
			streamString = (string)formatter.Deserialize(stream);
		}

		if(!streamString.Equals(string.Empty))
		{
			var desEncryption = new DESEncryption();
			string decryptedString = string.Empty;

			if(desEncryption.TryDecrypt (streamString, SUPER_SECRET, out decryptedString))
			{
				//Debug.Log(decryptedString);
				Instance._StoreData = JsonMapper.ToObject<Dictionary<string, object>> (decryptedString);
			}
			else
			{
				Debug.LogError("Could not decrypt save data.");
			}
		}
		else
		{
			Debug.LogWarning("Save data loaded empty string.");
		}
	}

	/// <summary>
	/// Save store data.
	/// </summary>
	public static void Save()
	{
		string jsonString = JsonMapper.ToJson (Instance._StoreData);
		var desEncryption = new DESEncryption();
		string encryptedString = desEncryption.Encrypt (jsonString, SUPER_SECRET);

		using (Stream stream = new FileStream(Filename(), FileMode.Create, FileAccess.Write, FileShare.None))																												//No compression
		{
			System.Runtime.Serialization.IFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
			formatter.Serialize(stream, encryptedString);
		}
	}

	/// <summary>
	/// Reset all data.
	/// </summary>
	public static void Reset()
	{
		Instance._StoreData.Clear ();
		Save ();
	}

	/// <summary>
	/// Return store data (in JSON format).
	/// </summary>
	public static string Print()
	{
		return JsonMapper.ToJson (Instance._StoreData);
	}

	/// <summary>
	/// Returns the store data, formatted.
	/// </summary>
	public static string PrintFormatted()
	{
		JsonReader reader = new JsonReader(JsonMapper.ToJson (Instance._StoreData));
		
		string text = string.Empty;
		
		while(reader.Read())
		{
			if(!reader.Token.ToString().Equals("ObjectStart") && !reader.Token.ToString().Equals("ObjectEnd"))
			{
				text += (reader.Value == null ? "null" : reader.Value.ToString()) + ": ";
				reader.Read();
				text += (reader.Value == null ? "null" : reader.Value.ToString()) + "\n";
			}
		}

		return text;
	}
}
