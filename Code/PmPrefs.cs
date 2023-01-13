using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class PmPrefs
{
  public static Dictionary<string, string> List = new Dictionary<string, string>();
  private static readonly string SaltKey = "xGLzdCWdJNEF7AEOGtWRtnNOZzObg4xa13MLLFZ1SbAy61ug4aCZQACypdN7UW1F";
  private static readonly string ViKey = "l9Qgcw8tYVksfNiP";
  private static string _secureKey;
  private static ICryptoTransform _decryptor;
  private static ICryptoTransform _encryptor;
  private static byte[] _keyBytes;
  private static bool _isStarted;
  private static byte[] _plainTextBytes;
  private static byte[] _cipherTextBytes;
  private static MemoryStream _memoryStream;
  private static CryptoStream _cryptoStream;
  private static int _decryptedByteCount;
  private static string _result;

  private static JsonSerializerSettings _jsonSettings;
  
  private static void Start()
  {
    _jsonSettings = new JsonSerializerSettings() {ReferenceLoopHandling = ReferenceLoopHandling.Ignore};
    
    _secureKey = PlayerPrefs.GetString("PMPREFS_SECURESTRING");
    _keyBytes = new Rfc2898DeriveBytes(PlayerPrefs.GetString("PMPREFS_SECURESTRING"), Encoding.ASCII.GetBytes(SaltKey)).GetBytes(32);
    RijndaelManaged rijndaelManaged1 = new RijndaelManaged();
    rijndaelManaged1.Mode = CipherMode.CBC;
    rijndaelManaged1.Padding = PaddingMode.Zeros;
    _encryptor = rijndaelManaged1.CreateEncryptor(_keyBytes, Encoding.ASCII.GetBytes(ViKey));
    RijndaelManaged rijndaelManaged2 = new RijndaelManaged();
    rijndaelManaged2.Mode = CipherMode.CBC;
    rijndaelManaged2.Padding = PaddingMode.None;
    _decryptor = rijndaelManaged2.CreateDecryptor(_keyBytes, Encoding.ASCII.GetBytes(ViKey));
    _isStarted = true;
  }

  public static void Save(string key, object value)
  {
    if (!_isStarted)
      Start();
    string str = JsonConvert.SerializeObject(value, _jsonSettings);
    
    if (List.ContainsKey(key))
      List[key] = str;
    else
      List.Add(key, str);
    
    SaveIt(key, str);
  }

  public static T Load<T>(string key)
  {
    if (!_isStarted)
      Start();

    TryGetData<T>(key, out var obj);
    
    return obj;
  }

  public static void DeleteAll()
  {
    if (!_isStarted)
      Start();
    
    _secureKey = PlayerPrefs.GetString("PMPREFS_SECURESTRING");
    PlayerPrefs.DeleteAll();
    PlayerPrefs.SetString("PMPREFS_SECURESTRING", _secureKey);
  }

  public static bool HasKey(string key) => PlayerPrefs.HasKey(key);

  public static void DeleteKey(string key)
  {
    if (!_isStarted)
      Start();
    
    PlayerPrefs.DeleteKey(key);
  }

  public static void SaveAll()
  {
    if (!_isStarted)
      Start();
    
    PlayerPrefs.Save();
  }

  public static string Encrypt(string plainText)
  {
    if (!_isStarted)
      Start();
    
    _plainTextBytes = Encoding.UTF8.GetBytes(plainText.Trim());
    using (_memoryStream = new MemoryStream())
    {
      using (_cryptoStream = new CryptoStream((Stream) _memoryStream, _encryptor, CryptoStreamMode.Write))
      {
        _cryptoStream.Write(_plainTextBytes, 0, _plainTextBytes.Length);
        _cryptoStream.FlushFinalBlock();
        _cipherTextBytes = _memoryStream.ToArray();
        _cryptoStream.Close();
      }
      _memoryStream.Close();
    }
    return Convert.ToBase64String(_cipherTextBytes);
  }

  public static string Decrypt(string encryptedText)
  {
    if (!_isStarted)
      Start();
    _cipherTextBytes = Convert.FromBase64String(encryptedText);
    _memoryStream = new MemoryStream(_cipherTextBytes);
    _cryptoStream = new CryptoStream((Stream) _memoryStream, _decryptor, CryptoStreamMode.Read);
    _plainTextBytes = new byte[_cipherTextBytes.Length];
    _decryptedByteCount = _cryptoStream.Read(_plainTextBytes, 0, _plainTextBytes.Length);
    _memoryStream.Close();
    _cryptoStream.Close();
    return Encoding.UTF8.GetString(_plainTextBytes, 0, _decryptedByteCount).TrimEnd("\0".ToCharArray());
  }

  private static void SaveIt(string key, string value)
  {
    if (!_isStarted)
      Start();
    if (!string.IsNullOrEmpty(_secureKey))
      value = Encrypt(value);
    PlayerPrefs.SetString(key, value);
  }

  private static string LoadIt(string key)
  {
    if (!_isStarted)
      Start();
    return PlayerPrefs.GetString(key);
  }

  private static void TryGetData<T>(string key, out T value)
  {
    if (!_isStarted)
      Start();
    _result = LoadIt(key);
    if (!string.IsNullOrEmpty(_secureKey))
      _result = Decrypt(_result);
    value = default (T);
    if (string.IsNullOrEmpty(_result))
      return;
    try
    {
      value = JsonConvert.DeserializeObject<T>(_result);
      if (List.ContainsKey(key))
        List[key] = _result;
      else
        List.Add(key, _result);
    }
    catch (Exception ex)
    {
      Debug.LogError((object) ("The value you try to load is corrupt: " + (object) ex));
    }
  }
}
