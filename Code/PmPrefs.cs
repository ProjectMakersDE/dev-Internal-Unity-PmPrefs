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
  private static string secureKey;
  private static ICryptoTransform decryptor;
  private static ICryptoTransform encryptor;
  private static byte[] keyBytes;
  private static bool isStarted;
  private static byte[] plainTextBytes;
  private static byte[] cipherTextBytes;
  private static MemoryStream memoryStream;
  private static CryptoStream cryptoStream;
  private static int decryptedByteCount;
  private static string result;

  private static JsonSerializerSettings _jsonSettings;
  
  private static void Start()
  {
    _jsonSettings = new JsonSerializerSettings() {ReferenceLoopHandling = ReferenceLoopHandling.Ignore};
    
    secureKey = PlayerPrefs.GetString("PMPREFS_SECURESTRING");
    keyBytes = new Rfc2898DeriveBytes(PlayerPrefs.GetString("PMPREFS_SECURESTRING"), Encoding.ASCII.GetBytes(SaltKey)).GetBytes(32);
    RijndaelManaged rijndaelManaged1 = new RijndaelManaged();
    rijndaelManaged1.Mode = CipherMode.CBC;
    rijndaelManaged1.Padding = PaddingMode.Zeros;
    encryptor = rijndaelManaged1.CreateEncryptor(keyBytes, Encoding.ASCII.GetBytes(ViKey));
    RijndaelManaged rijndaelManaged2 = new RijndaelManaged();
    rijndaelManaged2.Mode = CipherMode.CBC;
    rijndaelManaged2.Padding = PaddingMode.None;
    decryptor = rijndaelManaged2.CreateDecryptor(keyBytes, Encoding.ASCII.GetBytes(ViKey));
    isStarted = true;
  }

  public static void Save(string key, object value)
  {
    if (!isStarted)
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
    if (!isStarted)
      Start();
    
    T obj;
    TryGetData<T>(key, out obj);
    
    return obj;
  }

  public static void DeleteAll()
  {
    if (!isStarted)
      Start();
    
    secureKey = PlayerPrefs.GetString("PMPREFS_SECURESTRING");
    PlayerPrefs.DeleteAll();
    PlayerPrefs.SetString("PMPREFS_SECURESTRING", secureKey);
  }

  public static bool HasKey(string key) => PlayerPrefs.HasKey(key);

  public static void DeleteKey(string key)
  {
    if (!isStarted)
      Start();
    
    PlayerPrefs.DeleteKey(key);
  }

  public static void SaveAll()
  {
    if (!isStarted)
      Start();
    
    PlayerPrefs.Save();
  }

  public static string Encrypt(string plainText)
  {
    if (!isStarted)
      Start();
    
    plainTextBytes = Encoding.UTF8.GetBytes(plainText.Trim());
    using (memoryStream = new MemoryStream())
    {
      using (cryptoStream = new CryptoStream((Stream) memoryStream, encryptor, CryptoStreamMode.Write))
      {
        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
        cryptoStream.FlushFinalBlock();
        cipherTextBytes = memoryStream.ToArray();
        cryptoStream.Close();
      }
      memoryStream.Close();
    }
    return Convert.ToBase64String(cipherTextBytes);
  }

  public static string Decrypt(string encryptedText)
  {
    if (!isStarted)
      Start();
    cipherTextBytes = Convert.FromBase64String(encryptedText);
    memoryStream = new MemoryStream(cipherTextBytes);
    cryptoStream = new CryptoStream((Stream) memoryStream, decryptor, CryptoStreamMode.Read);
    plainTextBytes = new byte[cipherTextBytes.Length];
    decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
    memoryStream.Close();
    cryptoStream.Close();
    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount).TrimEnd("\0".ToCharArray());
  }

  private static void SaveIt(string key, string value)
  {
    if (!isStarted)
      Start();
    if (!string.IsNullOrEmpty(secureKey))
      value = Encrypt(value);
    PlayerPrefs.SetString(key, value);
  }

  private static string LoadIt(string key)
  {
    if (!isStarted)
      Start();
    return PlayerPrefs.GetString(key);
  }

  private static void TryGetData<T>(string key, out T value)
  {
    if (!isStarted)
      Start();
    result = LoadIt(key);
    if (!string.IsNullOrEmpty(secureKey))
      result = Decrypt(result);
    value = default (T);
    if (string.IsNullOrEmpty(result))
      return;
    try
    {
      value = JsonConvert.DeserializeObject<T>(result);
      if (List.ContainsKey(key))
        List[key] = result;
      else
        List.Add(key, result);
    }
    catch (Exception ex)
    {
      Debug.LogError((object) ("The value you try to load is corrupt: " + (object) ex));
    }
  }
}
