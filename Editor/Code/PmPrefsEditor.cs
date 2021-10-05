#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Win32;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PmPrefsEditor : EditorWindow
{
   private GUISkin _guiSkin;

   private GUIStyle _buttonStyle;
   private GUIStyle _buttonImgSStyle;
   private GUIStyle _buttonAdd;
   private GUIStyle _buttonShowUnityPrefs;
   private GUIStyle _buttonSkin;
   private GUIStyle _buttonDeleteAll;
   private GUIStyle _buttonSave;
   private GUIStyle _buttonRefresh;
   private GUIStyle _buttonDelete;
   private GUIStyle _buttonSort;
   private GUIStyle _buttonShowInPlainText;
   private GUIStyle _buttonImgLStyle;
   private GUIStyle _labelStyle;
   private GUIStyle _textFieldStyle;
   private GUIStyle _windowsStyle;
   private GUIStyle _horizontalSliderStyle;
   private GUIStyle _horizontalSliderTopStyle;
   private GUIStyle _titleTopStyle;

   private bool _creatingNewPp;
   private bool _darkTheme;
   private bool _showUnityPrefs;
   private bool _listSort;
   private bool _showItAsPlainText;

   private static string _path;
   private string _secureString;

   private List<PpDataStore> _playerPrefs;
   private PpDataStore _newPp;

   private Vector2 _scrollPos;
   private Vector2 _scrollPosCreateNew;

   private static Texture2D PmLogo => AssetDatabase.LoadAssetAtPath(Path + "logo.png", typeof(Texture2D)) as Texture2D;
   private Texture2D _backGround;

   private static List<string> _prefExceptions;

   private static List<string> UnityPrefs
   {
      get
      {
         var list = new List<string>();
         list.Add("unity.cloud_userid");
         list.Add("unity.player_sessionid");
         list.Add("UnityGraphicsQuality");
         list.Add("PackageUpdaterLastChecked");
         list.Add("unity.player_session_count");
         list.Add("PMPREFS_SECURESTRING");

         list.AddRange(_prefExceptions);

         return list;
      }
   }

   private Texture2D BackGround
   {
      get
      {
         var t = new Texture2D(0, 0);

         if (_backGround != null)
            t = _backGround;

         return t;
      }
      set => _backGround = value;
   }

   private Texture2D _emptyTexture2D;
   private string _exceptionsName;

   private static bool IsWinOs => Application.platform == RuntimePlatform.WindowsEditor;

   private static string Path
   {
      get
      {
         if (!string.IsNullOrEmpty(_path)) return _path;
         
         var res = Directory.GetFiles(Application.dataPath, "PmPrefsEditor.cs", SearchOption.AllDirectories);
         var assetPath = res[0].Replace("\\", "/");
         assetPath = "Assets" + assetPath.Replace(Application.dataPath, "");
         _path = Directory.GetParent(Directory.GetParent(assetPath).ToString()) + "/Textures/";

         return _path;
      }
   }

   [MenuItem("Tools/ProjectMakers/PmPrefs Editor")]
   private static void OpenWindow()
   {
      GetWindow<PmPrefsEditor>("PmPrefs").ChangeSkin();
   }

   private void GetPrefKeysMac()
   {
      var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/Library/Preferences/unity." + PlayerSettings.companyName + "." + PlayerSettings.productName + ".plist";

      var process = new Process();
      var processStartInfo = new ProcessStartInfo("plutil", "-convert xml1 \"" + folderPath + "\"");
      process.StartInfo = processStartInfo;
      process.Start();
      process.WaitForExit();
      var sr = new StreamReader(folderPath);
      var listData = sr.ReadToEnd();
      var xml = new XmlDocument();
      xml.LoadXml(listData);
      var list = xml["plist"];

      if (list == null) return;
      var node = list["dict"]?.FirstChild;

      while (node != null)
      {
         var innerText = node.InnerText;

         if (_showUnityPrefs)
            if (UnityPrefs.Any(s => innerText.Contains(s)))
               continue;

         node = node.NextSibling;
         var pref = new PpDataStore(innerText, node?.InnerText);
         node = node?.NextSibling;
         _playerPrefs.Add(pref);
      }

      Process.Start("plutil", " -convert binary1 \"" + folderPath + "\"");
   }

   private void GetPrefKeysWindows()
   {
      const string tempPath = @"Software\Unity\UnityEditor\";

      var key = Registry.CurrentUser.OpenSubKey(tempPath + PlayerSettings.companyName + @"\" + PlayerSettings.productName);

      if (key != null)
      {
         var list = key.GetValueNames().ToList();

         if (!_listSort)
            list.Sort();
         else
            list.Sort((a, b) => -1 * string.Compare(a, b, StringComparison.Ordinal));

         foreach (var subkey in list)
         {
            var keyName = subkey.Substring(0, subkey.LastIndexOf("_", StringComparison.Ordinal));

            if (_showUnityPrefs)
               if (UnityPrefs.Any(s => keyName.Contains(s)))
                  continue;

            if (keyName.Contains("PMPREFS_SECURESTRING"))
               continue;

            var val = key.GetValue(subkey);

            if (val.GetType() != typeof(int) && val.GetType() != typeof(float))
               val = Encoding.ASCII.GetString((byte[]) val);

            var str = val.ToString();

            if (!string.IsNullOrEmpty(PlayerPrefs.GetString("PMPREFS_SECURESTRING")))
            {
               if (_showItAsPlainText)
               {
                  if (!UnityPrefs.Any(s => keyName.Contains(s)))
                  {
                     try
                     {
                        str = PmPrefs.Decrypt(PlayerPrefs.GetString(keyName));
                     }
                     catch (Exception e)
                     {
                        Debug.LogError("It's not the correct Value: " + keyName + ":" + PlayerPrefs.GetString(keyName) + " => " + e);
                     }
                  }
               }
            }

            var pref = new PpDataStore(keyName, str);
            _playerPrefs.Add(pref);
         }
      }
   }

   private void ChangeSkin()
   {
      if (!_darkTheme)
      {
         _guiSkin = AssetDatabase.LoadAssetAtPath(Path + "skin.guiskin", typeof(GUISkin)) as GUISkin;
         BackGround = AssetDatabase.LoadAssetAtPath(Path + "bg.png", typeof(Texture2D)) as Texture2D;
      }
      else
      {
         _guiSkin = AssetDatabase.LoadAssetAtPath(Path + "skin_light.guiskin", typeof(GUISkin)) as GUISkin;
         BackGround = AssetDatabase.LoadAssetAtPath(Path + "bg_w.png", typeof(Texture2D)) as Texture2D;
      }

      if (_guiSkin != null)
      {
         _buttonStyle = _guiSkin.GetStyle("button");
         _buttonAdd = _guiSkin.GetStyle("buttonadd");
         _buttonShowUnityPrefs = _guiSkin.GetStyle("buttonunityprefs");
         _buttonSkin = _guiSkin.GetStyle("buttonskin");
         _buttonDeleteAll = _guiSkin.GetStyle("buttondeleteall");
         _buttonSave = _guiSkin.GetStyle("buttonsave");
         _buttonRefresh = _guiSkin.GetStyle("buttonrefresh");
         _buttonDelete = _guiSkin.GetStyle("buttondelete");
         _buttonSort = _guiSkin.GetStyle("buttonsort");
         _buttonShowInPlainText = _guiSkin.GetStyle("buttonshowasplaintext");
         _labelStyle = _guiSkin.GetStyle("label");
         _textFieldStyle = _guiSkin.GetStyle("textfield");
         _horizontalSliderStyle = _guiSkin.GetStyle("horizontalslider");
         _horizontalSliderTopStyle = _guiSkin.GetStyle("horizontalslidertop");
         _titleTopStyle = _guiSkin.GetStyle("title");
      }

      _secureString = PlayerPrefs.GetString("PMPREFS_SECURESTRING");
   }

   private void OnGUI()
   {
      LoadEditorPrefs();

      if (_guiSkin == null)
         ChangeSkin();

      GUI.DrawTexture(new Rect(0, 0, position.width, position.height), BackGround);

      if (_emptyTexture2D == null)
         _emptyTexture2D = new Texture2D(0, 0);

      if (_playerPrefs == null)
      {
         RefreshPlayerPrefs();
      }

      if (_guiSkin == null)
      {
         try
         {
            ChangeSkin();
         }
         catch
         {
            Debug.LogError("There is no skin in the \"Texture\" folder, please reimport the PmPrefs Asset.");
         }
      }

      GUILayout.Space(20);
      GUILayout.BeginHorizontal();
      GUILayout.FlexibleSpace();
      GUILayout.Label(PmLogo);
      GUILayout.FlexibleSpace();
      GUILayout.EndHorizontal();
      GUILayout.Space(12);
      EditorGUILayout.LabelField(string.Empty, _horizontalSliderTopStyle);
      GUILayout.Space(-17);
      GUILayout.BeginHorizontal();

      if (GUILayout.Button(new GUIContent(new Texture2D(0, 0), "Create new value."), _buttonAdd))
      {
         GUIUtility.keyboardControl = 0;
         _newPp = new PpDataStore(string.Empty, string.Empty);
         _creatingNewPp = !_creatingNewPp;
      }

      if (GUILayout.Button(new GUIContent(new Texture2D(0, 0), "Sort PmPrefs by name."), _buttonSort))
      {
         GUIUtility.keyboardControl = 0;
         _listSort = !_listSort;
         RefreshPlayerPrefs();
      }

      if (GUILayout.Button(new GUIContent(new Texture2D(0, 0), "Show or hide specific PlayerPrefs."), _buttonShowUnityPrefs))
      {
         GUIUtility.keyboardControl = 0;
         _showUnityPrefs = !_showUnityPrefs;
         RefreshPlayerPrefs();
      }

      if (GUILayout.Button(new GUIContent(new Texture2D(0, 0), "Show with or without encoding. (Regardless of whether the encryption is active or inactive)"), _buttonShowInPlainText))
      {
         GUIUtility.keyboardControl = 0;
         _showItAsPlainText = !_showItAsPlainText;
         RefreshPlayerPrefs();
      }

      GUILayout.FlexibleSpace();

      if (GUILayout.Button(new GUIContent(new Texture2D(0, 0), "Change the skin in dark or light."), _buttonSkin))
      {
         GUIUtility.keyboardControl = 0;
         _darkTheme = !_darkTheme;
         ChangeSkin();
      }

      GUILayout.FlexibleSpace();

      if (GUILayout.Button(new GUIContent(new Texture2D(0, 0), "Delete all values."), _buttonDeleteAll))
      {
         GUIUtility.keyboardControl = 0;
         if (EditorUtility.DisplayDialog(
            "Delete PmPrefs?",
            "Delete all PmPrefs? This cannot be undone.",
            "Yes",
            "No"))
         {
            PmPrefs.DeleteAll();
            RefreshPlayerPrefs();
         }
      }

      if (GUILayout.Button(new GUIContent(_emptyTexture2D, "Save all changes."), _buttonSave))
      {
         GUIUtility.keyboardControl = 0;
         SaveAll();
         RefreshPlayerPrefs();
      }

      if (GUILayout.Button(new GUIContent(_emptyTexture2D, "Reload all PmPrefs."), _buttonRefresh))
      {
         GUIUtility.keyboardControl = 0;
         RefreshPlayerPrefs();
      }

      GUILayout.EndHorizontal();

      if (_creatingNewPp)
      {
         if (_newPp == null)
         {
            _newPp = new PpDataStore(string.Empty, string.Empty);
         }

         GUILayout.Space(-4);
         EditorGUILayout.LabelField(string.Empty, _horizontalSliderStyle);

         GUILayout.BeginArea(new Rect(40, 164, position.width - 80, 400));
         GUILayout.BeginHorizontal();
         GUILayout.FlexibleSpace();
         GUILayout.Label("Create PmPref", _titleTopStyle);
         GUILayout.FlexibleSpace();
         GUILayout.EndHorizontal();
         GUILayout.Space(8);
         GUILayout.BeginHorizontal();
         EditorGUILayout.LabelField("Name:", _labelStyle, GUILayout.Width(128));
         _newPp.Name = EditorGUILayout.TextField(_newPp.Name, _textFieldStyle);
         GUILayout.EndHorizontal();
         GUILayout.BeginHorizontal();
         EditorGUILayout.LabelField("Value:", _labelStyle, GUILayout.Width(128));
         _newPp.Value.StringValue = EditorGUILayout.TextField(_newPp.Value.StringValue, _textFieldStyle);

         GUILayout.EndHorizontal();
         GUILayout.Space(4);
         GUILayout.BeginHorizontal();
         GUILayout.FlexibleSpace();

         if (GUILayout.Button("Create", _buttonStyle))
         {
            GUIUtility.keyboardControl = 0;
            PmPrefs.Save(_newPp.Name, _newPp.StringValue);
            RefreshPlayerPrefs();
         }

         if (GUILayout.Button("Cancel", _buttonStyle))
         {
            GUIUtility.keyboardControl = 0;
            _creatingNewPp = false;
         }

         GUILayout.Space(5);
         GUILayout.EndHorizontal();
         GUILayout.Space(5);

         // Create PmPref exception
         GUILayout.BeginHorizontal();
         GUILayout.FlexibleSpace();
         GUILayout.Label("Create PmPref exception", _titleTopStyle);
         GUILayout.FlexibleSpace();
         GUILayout.EndHorizontal();
         GUILayout.Space(8);
         GUILayout.BeginHorizontal();
         EditorGUILayout.LabelField("Name:", _labelStyle, GUILayout.Width(128));
         _exceptionsName = EditorGUILayout.TextField(_exceptionsName, _textFieldStyle);
         GUILayout.EndHorizontal();
         GUILayout.Space(4);
         GUILayout.BeginHorizontal();
         GUILayout.FlexibleSpace();

         if (GUILayout.Button("Create", _buttonStyle))
         {
            GUIUtility.keyboardControl = 0;
            if (_prefExceptions == null)
               _prefExceptions = new List<string>();

            _prefExceptions.Add(_exceptionsName);
            SaveEditorPrefs();
         }

         if (GUILayout.Button("Cancel", _buttonStyle))
         {
            GUIUtility.keyboardControl = 0;
            _creatingNewPp = false;
         }

         GUILayout.Space(5);
         GUILayout.EndHorizontal();
         GUILayout.Space(5);

         // Show PmPref Exception List
         GUILayout.BeginHorizontal();
         GUILayout.FlexibleSpace();
         GUILayout.Label("PmPref exception List", _titleTopStyle);
         GUILayout.FlexibleSpace();
         GUILayout.EndHorizontal();
         GUILayout.Space(8);

         _scrollPosCreateNew = GUILayout.BeginScrollView(_scrollPosCreateNew);

         var deleteList = new List<string>();
         if (_prefExceptions != null)
         {
            foreach (var s in _prefExceptions)
            {
               GUILayout.BeginHorizontal(GUILayout.Height(34));
               GUILayout.FlexibleSpace();
               GUILayout.Label(s, _labelStyle);
               GUILayout.Space(16);

               if (GUILayout.Button(new GUIContent(_emptyTexture2D, "Delete this"), _buttonDelete))
               {
                  GUIUtility.keyboardControl = 0;
                  deleteList.Add(s);
               }

               GUILayout.FlexibleSpace();
               GUILayout.EndHorizontal();
            }
         }

         if (deleteList.Count >= 1)
         {
            foreach (var s in deleteList)
            {
               _prefExceptions?.Remove(s);
            }

            SaveEditorPrefs();
         }

         GUILayout.EndScrollView();

         GUILayout.EndArea();
         GUILayout.Space(410);
      }

      GUILayout.Space(-4);
      EditorGUILayout.LabelField(string.Empty, _horizontalSliderStyle);
      _scrollPos = GUILayout.BeginScrollView(_scrollPos);

      if (_playerPrefs.Count == 0)
      {
         GUILayout.Label("No PlayerPrefs", _labelStyle);
      }
      else
      {
         foreach (var t in _playerPrefs)
         {
            GUILayout.BeginHorizontal(GUILayout.Height(34));
            GUILayout.Space(10);

            if (t.DeleteMarker)
            {
               GUI.color = Color.red;
            }
            else if (t.Changed)
            {
               GUI.color = Color.green;
            }
            else
            {
               GUI.color = Color.white;
            }

            GUILayout.Label(t.Name, _labelStyle, GUILayout.Width(160));
            t.Value.StringValue = EditorGUILayout.TextField(t.Value.StringValue, _textFieldStyle, GUILayout.MaxWidth(512));

            GUI.color = Color.white;
            GUILayout.BeginVertical();
            GUILayout.Space(5);
            if (GUILayout.Button(new GUIContent(_emptyTexture2D, "Delete this PmPref."), _buttonDelete))
            {
               GUIUtility.keyboardControl = 0;
               t.DeleteMarker = !t.DeleteMarker;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
         }
      }

      GUILayout.Space(8);
      EditorGUILayout.LabelField(string.Empty, _horizontalSliderStyle);
      GUILayout.BeginHorizontal();

      GUILayout.FlexibleSpace();
      GUILayout.Label("Configuration", _titleTopStyle);
      GUILayout.FlexibleSpace();
      GUILayout.EndHorizontal();

      GUILayout.BeginHorizontal(GUILayout.Height(34));
      GUILayout.Space(40);
      EditorGUILayout.LabelField("Secure Key:", _labelStyle, GUILayout.Width(96));
      _secureString = EditorGUILayout.TextField(_secureString, _textFieldStyle);

      if (GUILayout.Button("SetKey", _buttonStyle))
      {
         GUIUtility.keyboardControl = 0;
         if (EditorUtility.DisplayDialog(
            "Change SecureKey",
            "You really want to change the Secure Key? The encryption of all actually saved PmPrefs will be changed! This cannot be undone! (It's better to delete all PmPrefs before change the SecureKey)",
            "Yes",
            "No"))
         {
            ChangeSecureKey();
         }
      }

      GUILayout.Space(40);
      GUILayout.EndHorizontal();
      GUILayout.BeginHorizontal();
      GUILayout.FlexibleSpace();

      if (GUILayout.Button(new GUIContent(
            "Export",
            "Exports all currently visible PmPrefs variables.\nImportant: Only the variables which are displayed are exported! Likewise, they are exported encrypted or decrypted, depending on how it is displayed in the PmPrefs Editor."),
         _buttonStyle,
         GUILayout.Width(128)))
      {
         var fileName = EditorUtility.SaveFilePanel("Export Folder", Application.absoluteURL, DateTime.Now.ToShortDateString() + "_PmPrefs_Export", "csv");
         if (!string.IsNullOrEmpty(fileName))
            Export(fileName);
      }

      if (GUILayout.Button(new GUIContent(
            "Import",
            "Imports PmPrefs data from a csv file. Important: the Import overwrites all existing PmPrefs and encrypts the new data with the new SecureKey (if a SecureKey is currently specified)."),
         _buttonStyle,
         GUILayout.Width(128)))
      {
         var fileName = EditorUtility.OpenFilePanel("Import Folder", Application.absoluteURL, "csv");
         if (!string.IsNullOrEmpty(fileName))
            Import(fileName);
      }

      GUILayout.FlexibleSpace();
      GUILayout.EndHorizontal();
      GUILayout.Space(10);
      GUILayout.EndScrollView();
      
      GUILayout.FlexibleSpace();
      GUILayout.Space(16);
      GUILayout.BeginHorizontal();
        
      if (GUILayout.Button("ProjectMakers.de",EditorStyles.toolbarButton))
      {
         Application.OpenURL("https://projectmakers.de");
      }

      GUILayout.EndHorizontal();
   }

   private static void SaveEditorPrefs()
   {
      var ex = "";
      foreach (var s in _prefExceptions)
      {
         if (ex == "")
            ex += s;
         else
            ex += ";" + s;
      }

      EditorPrefs.SetString("PmPrefs_Exception", ex);
   }

   private static void LoadEditorPrefs()
   {
      _prefExceptions = new List<string>();
      var ex = EditorPrefs.GetString("PmPrefs_Exception").Split(';');

      foreach (var s in ex)
      {
         if (!string.IsNullOrEmpty(s))
         {
            _prefExceptions.Add(s);
         }
      }
   }

   private void RefreshPlayerPrefs()
   {
      _playerPrefs?.Clear();

      _secureString = PlayerPrefs.GetString("PMPREFS_SECURESTRING");

      _playerPrefs = new List<PpDataStore>();

      if (IsWinOs)
         GetPrefKeysWindows();
      else
         GetPrefKeysMac();
   }

   private void SaveAll()
   {
      for (var i = _playerPrefs.Count - 1; i >= 0; i--)
      {
         var pref = _playerPrefs[i];

         if (pref.DeleteMarker)
         {
            PmPrefs.DeleteKey(pref.Name);
            _playerPrefs.RemoveAt(i);
            continue;
         }

         if (pref.Changed)
         {
            var s = pref.Value.StringValue;
            s = PmPrefs.Encrypt(s);
            try
            {
               PlayerPrefs.SetString(pref.Name, s);
               pref.Save();
            }
            catch (Exception e)
            {
               Debug.LogError("The value you have changed is not the correct format! - " + e);
            }
         }
      }
   }

   private void ChangeSecureKey()
   {
      var encoded = !string.IsNullOrEmpty(PlayerPrefs.GetString("PMPREFS_SECURESTRING"));
      string str;

      if (encoded)
      {
         for (var i = _playerPrefs.Count - 1; i >= 0; i--)
         {
            if (UnityPrefs.Any(s => _playerPrefs[i].Name.Contains(s)))
               continue;

            str = PlayerPrefs.GetString(_playerPrefs[i].Name);
            str = PmPrefs.Decrypt(str);
            PlayerPrefs.SetString(_playerPrefs[i].Name, str);
         }
      }

      PlayerPrefs.SetString("PMPREFS_SECURESTRING", _secureString);
      encoded = !string.IsNullOrEmpty(PlayerPrefs.GetString("PMPREFS_SECURESTRING"));

      for (var i = _playerPrefs.Count - 1; i >= 0; i--)
      {
         if (UnityPrefs.Any(s => _playerPrefs[i].Name.Contains(s)))
            continue;

         str = PlayerPrefs.GetString(_playerPrefs[i].Name);

         if (encoded)
            str = PmPrefs.Encrypt(PlayerPrefs.GetString(_playerPrefs[i].Name));

         PlayerPrefs.SetString(_playerPrefs[i].Name, str);
      }

      RefreshPlayerPrefs();
   }

   private void Export(string exportPath)
   {
      var encoded = !string.IsNullOrEmpty(PlayerPrefs.GetString("PMPREFS_SECURESTRING"));
      var csv = "";
      for (var i = _playerPrefs.Count - 1; i >= 0; i--)
      {
         var value = PlayerPrefs.GetString(_playerPrefs[i].Name);

         if (!encoded || !UnityPrefs.Any(s => _playerPrefs[i].Name.Contains(s)))
            value = PmPrefs.Decrypt(PlayerPrefs.GetString(_playerPrefs[i].Name));

         csv += _playerPrefs[i].Name + ";" + value + Environment.NewLine;
      }

      try
      {
         File.WriteAllText(exportPath, csv);
      }
      catch (Exception e)
      {
         Debug.LogError("It's not possible to save the Export: " + e);
      }
   }

   private void Import(string importPath)
   {
      var encoded = !string.IsNullOrEmpty(PlayerPrefs.GetString("PMPREFS_SECURESTRING"));
      var reader = new StreamReader(File.OpenRead(importPath));
      while (!reader.EndOfStream)
      {
         var line = reader.ReadLine();
         if (!string.IsNullOrWhiteSpace(line))
         {
            var sa = line.Split(';');
            var key = sa[0];
            var value = sa[1];

            if (encoded && UnityPrefs.Any(s => key.Contains(s)))
               continue;

            value = PmPrefs.Encrypt(value);

            PlayerPrefs.SetString(key, value);
         }
      }
   }
}

public class PpDataStore
{
   public bool DeleteMarker;
   private PrefValue _initial;
   public string Name;
   public PrefValue Value;

   public PpDataStore(string name, string valueTxt)
   {
      Name = name;
      Value = new PrefValue();
      _initial = new PrefValue();
      Value.StringValue = _initial.StringValue = valueTxt;
   }

   public bool Changed => Value.StringValue != _initial.StringValue;

   public string StringValue => Value.StringValue;

   public void Save()
   {
      _initial.StringValue = Value.StringValue;
   }

   public class PrefValue
   {
      public string StringValue;
   }
}
#endif