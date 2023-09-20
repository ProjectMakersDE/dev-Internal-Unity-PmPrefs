using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PM.Plugins
{
   public static class PmPrefs
   {
      private static List<string> _list;

      private const string SaltKey = "F1m5eJVO9ASPxGW7B3KP9t8iNd5Edpb48LAGNlWcLHeNkeH6PNYf3BCztZB7D3ch";
      private const string ViKey = "NiB3KP9VksfNf3Bi";
      private const string SecureKey = "LoKo1Nibu75XXzu";
      private const string KeyList = "PmPrefs_KeyList";
      private const string Prefix = "PmPrefs__";

      private static byte[] _keyBytes = new Rfc2898DeriveBytes(SecureKey, Encoding.ASCII.GetBytes(SaltKey)).GetBytes(32);

      private static ICryptoTransform _decryptor;
      private static ICryptoTransform _encryptor;

      private static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };

      private static string _oldSecureKey;

      private static List<string> List
      {
         get
         {
            if (_list == null && HasKey(KeyList))
               _list = JsonConvert.DeserializeObject<List<string>>(PlayerPrefs.GetString(KeyList), _jsonSettings);

            return _list ?? new List<string>();
         }
      }

      private static void AddKeyToList(string key)
      {
         if (List.Contains(key)) return;

         List.Add(key);
         _list = List;
         PlayerPrefs.SetString(KeyList, JsonConvert.SerializeObject(_list));
      }

      private static ICryptoTransform Decryptor()
      {
         if (_decryptor != null && SecureKey == _oldSecureKey) return _decryptor;

         var rm = new RijndaelManaged { Mode = CipherMode.CBC, Padding = PaddingMode.None };
         _decryptor = rm.CreateDecryptor(_keyBytes, Encoding.ASCII.GetBytes(ViKey));

         _oldSecureKey = SecureKey;
         return _decryptor;
      }

      private static ICryptoTransform Encryptor()
      {
         if (_encryptor != null && SecureKey == _oldSecureKey) return _encryptor;

         var rm = new RijndaelManaged { Mode = CipherMode.CBC, Padding = PaddingMode.Zeros };
         _encryptor = rm.CreateEncryptor(_keyBytes, Encoding.ASCII.GetBytes(ViKey));

         _oldSecureKey = SecureKey;
         return _encryptor;
      }

      public static void DeleteAll() => PlayerPrefs.DeleteAll();
      public static bool HasKey(string key) => PlayerPrefs.HasKey(Prefix + key);
      public static void DeleteKey(string key) => PlayerPrefs.DeleteKey(Prefix + key);
      public static void SaveAll() => PlayerPrefs.Save();
      public static List<string> GetAllKeys() => List;

      public static void Save<T>(T key, object value)
      {
         Save(key.ToString(), value);
      }

      public static void Save(string key, object value)
      {
         string str = JsonConvert.SerializeObject(value, _jsonSettings);
         AddKeyToList(key);
         SaveIt(key, str);
      }
      public static T Load<TK, T>(TK key, T defaultValue = default)
      {
         return TryGetData(key.ToString(), out T obj) ? obj : defaultValue;
      }

      public static T Load<T>(string key, T defaultValue = default)
      {
         return TryGetData(key, out T obj) ? obj : defaultValue;
      }

      public static string Encrypt(string plainText)
      {
         var plainTextBytes = Encoding.UTF8.GetBytes(plainText.Trim());

         using var memoryStream = new MemoryStream();
         using var cryptoStream = new CryptoStream(memoryStream, Encryptor(), CryptoStreamMode.Write);

         cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
         cryptoStream.FlushFinalBlock();
         var cipherTextBytes = memoryStream.ToArray();

         return Convert.ToBase64String(cipherTextBytes);
      }

      public static string Decrypt(string encryptedText)
      {
         var cipherTextBytes = Convert.FromBase64String(encryptedText);
         var plainTextBytes = new byte[cipherTextBytes.Length];

         using var memoryStream = new MemoryStream(cipherTextBytes);
         using var cryptoStream = new CryptoStream(memoryStream, Decryptor(), CryptoStreamMode.Read);

         var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
         return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount).TrimEnd("\0".ToCharArray());
      }

      private static void SaveIt(string key, string value)
      {
         if (key == KeyList) return;

         PlayerPrefs.SetString(Prefix + key, Encrypt(value));
      }

      private static bool TryGetData<T>(string key, out T value)
      {
         value = default;

         if (key == KeyList)
            return false;

         var result = PlayerPrefs.GetString(Prefix + key);

         result = Decrypt(result);

         if (string.IsNullOrEmpty(result))
            return false;

         try
         {
            value = JsonConvert.DeserializeObject<T>(result);
            return true;
         }
         catch (Exception ex)
         {
            Debug.LogError("The value you try to load is corrupt: " + ex);
            return false;
         }
      }
   }
}