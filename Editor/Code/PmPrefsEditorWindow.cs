#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PM.Plugins
{
   public class PmPrefsEditorWindow : EditorWindow
   {
      private VisualElement _root;

      private VisualTreeAsset _visualTreePmPrefsListItem;
      private VisualTreeAsset _visualTreePlayerPrefsListItem;

      private ListView _listViewPmPrefsList;
      private ListView _listViewPlayerPrefsList;

      private VisualElement _createNewContainer;
      private VisualElement _configurationContainer;

      private Button _saveButton;
      private Button _deleteAllButton;
      private Button _createNewButton;
      private Button _createButton;
      private Button _configurationButton;
      private Button _showEncryptedButton;
      private Button _refreshButton;
      private Button _changeSecureKeyButton;

      private Button _showPmPrefsButton;
      private Button _showPlayerPrefsButton;

      private Button _exportButton;
      private Button _importButton;

      private TextField _createNewKeyField;
      private TextField _createNewValueField;

      private TextField _changeSecureKeyField;

      private ToolbarSearchField _searchField;

      private bool _showItAsPlainText;
      private bool _listSort;

      private bool _showCreateNew;
      private bool _showConfig;

      public bool ShowEncrypted;

      public const string Prefix = "PmPrefs__";

      public List<PmPrefsListItem> PlayerPrefsList;
      public List<PmPrefsListItem> PmPrefsList;
      private readonly GetWindowsKeys _getWindowsKeys;

      public PmPrefsEditorWindow()
      {
         _getWindowsKeys = new GetWindowsKeys(this);
      }

      private GetWindowsKeys GetWindowsKeys => _getWindowsKeys;

      [MenuItem("Tools/ProjectMakers/PmPrefs")]
      public static void ShowExample()
      {
         PmPrefsEditorWindow wnd = GetWindow<PmPrefsEditorWindow>();
         wnd.titleContent = new GUIContent("PmPrefs");
         wnd.minSize = new Vector2(380, 356);
      }

      private void Initialize()
      {
         rootVisualElement.Clear();
         _root = rootVisualElement;

         var visualTree = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath("Packages/com.projectmakers.pmprefs/Editor/Style/PmPrefs.uxml", typeof(VisualTreeAsset));

         var labelFromUxml = visualTree.Instantiate();
         _root.Add(labelFromUxml);

         InitializeVisualElements();
         GetKeys();

         _root.MarkDirtyRepaint();
         Repaint();
      }

      public void CreateGUI()
      {
         Initialize();
      }

      private void InitializeVisualElements()
      {
         _visualTreePmPrefsListItem = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath("Packages/com.projectmakers.pmprefs/Editor/Style/PmPrefsListItem.uxml", typeof(VisualTreeAsset));

         _listViewPmPrefsList = _root.Q<ListView>("PmPrefsList");
         _listViewPlayerPrefsList = _root.Q<ListView>("PlayerPrefsList");

         _saveButton = _root.Q<Button>("Save_btn");
         _deleteAllButton = _root.Q<Button>("DeleteAll_btn");
         _createNewButton = _root.Q<Button>("CreateNew_btn");
         _createButton = _root.Q<Button>("Create_btn");
         _changeSecureKeyButton = _root.Q<Button>("ChangeSecureKey_btn");

         _createNewKeyField = _root.Q<TextField>("CreateName_tf");
         _createNewValueField = _root.Q<TextField>("CreateValue_tf");
         _changeSecureKeyField = _root.Q<TextField>("ChangeSecureKey_tf");

         _refreshButton = _root.Q<Button>("Refresh_btn");
         _showEncryptedButton = _root.Q<Button>("ShowEncryp_btn");

         _configurationButton = _root.Q<Button>("Configuration_btn");
         _showPmPrefsButton = _root.Q<Button>("PmPrefs_btn");
         _showPlayerPrefsButton = _root.Q<Button>("PlayerPrefs_btn");

         _exportButton = _root.Q<Button>("Export_btn");
         _importButton = _root.Q<Button>("Import_btn");

         _createNewContainer = _root.Q<VisualElement>("Create");
         _configurationContainer = _root.Q<VisualElement>("Configuration");

         _searchField = _root.Q<ToolbarSearchField>("search_field");

         _saveButton.clicked += SaveAll;

         _deleteAllButton.clicked += OnDeleteAllButtonOnClicked;

         _exportButton.clicked += () =>
         {
            var path = EditorUtility.SaveFilePanel("Export Folder", Application.absoluteURL, DateTime.Now.ToShortDateString() + "_PmPrefs_Export", "csv");

            if (path.Length != 0)
               Export(path);
         };

         _importButton.clicked += () =>
         {
            var path = EditorUtility.OpenFilePanel("Import Folder", Application.absoluteURL, "csv");

            if (path.Length != 0)
               Import(path);
         };

         _createNewButton.clicked += OnCreateNewButtonOnClicked;
         _createButton.clicked += Create;

         _changeSecureKeyButton.clicked += ChangeSecureKey;

         _configurationButton.clicked += OnConfigurationButtonOnClicked;
         _refreshButton.clicked += GetKeys;

         _showEncryptedButton.clicked += () =>
         {
            ShowEncrypted = !ShowEncrypted;
            _showEncryptedButton.style.backgroundColor = ShowEncrypted ? new StyleColor(new Color(.15f, .15f, .15f)) : new StyleColor(new Color(.235f, .235f, .235f));
            Initialize();
         };

         _showPmPrefsButton.clicked += OnShowPmPrefsButtonOnClicked;
         _showPlayerPrefsButton.clicked += OnShowPlayerPrefsButtonOnClicked;
      }

      private void OnDeleteAllButtonOnClicked()
      {
         if (EditorUtility.DisplayDialog("Delete All Keys", "Are you sure you want to delete all PmPrefs and PlayerPrefs?", "Yes", "No"))
         {
            PmPrefs.DeleteAll();
            PmPrefs.SaveAll();

            PmPrefsList.Clear();
            PlayerPrefsList.Clear();

            _listViewPmPrefsList.Clear();
            _listViewPlayerPrefsList.Clear();

            Initialize();
         }
      }

      private void Export(string path)
      {
         var csv = "";

         for (var i = PmPrefsList.Count - 1; i >= 0; i--)
         {
            var value = ShowEncrypted ? PmPrefs.Decrypt(PmPrefsList[i].Value) : PmPrefsList[i].Value;
            csv += "PmPrefs;" + PmPrefsList[i].Key + ";" + value + Environment.NewLine;
         }

         for (var i = PlayerPrefsList.Count - 1; i >= 0; i--)
         {
            csv += "PlayerPrefs;" + PlayerPrefsList[i].Key + ";" + PlayerPrefsList[i].Value + Environment.NewLine;
         }

         File.WriteAllText(path, csv);
      }

      private void Import(string importPath)
      {
         PmPrefs.DeleteAll();
         PmPrefs.SaveAll();

         PmPrefsList.Clear();
         PlayerPrefsList.Clear();

         _listViewPmPrefsList.Clear();
         _listViewPlayerPrefsList.Clear();

         var reader = new StreamReader(File.OpenRead(importPath));

         while (!reader.EndOfStream)
         {
            var line = reader.ReadLine();
            if (!string.IsNullOrWhiteSpace(line))
            {
               var sa = line.Split(';');
               var type = sa[0];
               var key = sa[1];
               var value = sa[2];

               if (type == "PmPrefs")
               {
                  value = PmPrefs.Encrypt(value);
                  key = Prefix + key;
               }

               PlayerPrefs.SetString(key, value);
            }
         }

         Initialize();
      }

      private void OnConfigurationButtonOnClicked()
      {
         if (!_showConfig)
         {
            _configurationContainer.style.display = DisplayStyle.Flex;
            _configurationButton.style.backgroundColor = new StyleColor(new Color(.15f, .15f, .15f));

            _createNewContainer.style.display = DisplayStyle.None;
            _createNewButton.style.backgroundColor = new StyleColor(new Color(.235f, .235f, .235f));

            _showConfig = true;
            _showCreateNew = false;
         }
         else
         {
            _configurationContainer.style.display = DisplayStyle.None;
            _configurationButton.style.backgroundColor = new StyleColor(new Color(.235f, .235f, .235f));
            _showConfig = false;
         }
      }

      private void OnCreateNewButtonOnClicked()
      {
         if (!_showCreateNew)
         {
            _createNewContainer.style.display = DisplayStyle.Flex;
            _createNewButton.style.backgroundColor = new StyleColor(new Color(.15f, .15f, .15f));

            _configurationContainer.style.display = DisplayStyle.None;
            _configurationButton.style.backgroundColor = new StyleColor(new Color(.235f, .235f, .235f));
            _showConfig = false;
            _showCreateNew = true;
         }
         else
         {
            _createNewContainer.style.display = DisplayStyle.None;
            _createNewButton.style.backgroundColor = new StyleColor(new Color(.235f, .235f, .235f));
            _showCreateNew = false;
         }
      }

      private void OnShowPlayerPrefsButtonOnClicked()
      {
         _listViewPmPrefsList.style.display = DisplayStyle.None;
         _showPlayerPrefsButton.style.backgroundColor = new StyleColor(new Color(.15f, .15f, .15f));
         _listViewPlayerPrefsList.style.display = DisplayStyle.Flex;
         _showPmPrefsButton.style.backgroundColor = new StyleColor(new Color(.235f, .235f, .235f));
      }

      private void OnShowPmPrefsButtonOnClicked()
      {
         _listViewPmPrefsList.style.display = DisplayStyle.Flex;
         _showPmPrefsButton.style.backgroundColor = new StyleColor(new Color(.15f, .15f, .15f));
         _listViewPlayerPrefsList.style.display = DisplayStyle.None;
         _showPlayerPrefsButton.style.backgroundColor = new StyleColor(new Color(.235f, .235f, .235f));
      }

      private void Create()
      {
         if (PmPrefsList.Exists(t => t.Key == _createNewKeyField.text)) return;

         var listValue = ShowEncrypted ? PmPrefs.Encrypt(_createNewValueField.text) : _createNewValueField.text;

         PmPrefsList.Add(new PmPrefsListItem(_createNewKeyField.text, listValue));
         PmPrefs.Save(_createNewKeyField.text, _createNewValueField.text);
         PmPrefs.SaveAll();

         Initialize();

         if (PmPrefsList.Count == 1)
            EditorUtility.DisplayDialog("Warning", "Only one Element is Saved with PmPrefs.\nCurrently the list viewer in Unity still has a bug, so that a single entry is not displayed. ", "OK");
      }

      private void FillList(ListView l, List<PmPrefsListItem> p)
      {
         l.Clear();

         l.makeItem = () =>
         {
            var newListEntry = _visualTreePmPrefsListItem.Instantiate();
            var newListEntryLogic = new PmPrefsListItemEntryController();

            newListEntry.userData = newListEntryLogic;
            newListEntryLogic.SetVisualElement(newListEntry);

            return newListEntry;
         };

         l.bindItem = (item, index) => { ((PmPrefsListItemEntryController)item.userData).SetData(p[index]); };

         l.itemsSource = p;
      }

      private void SaveAll()
      {
         var pmPrefsList = PmPrefsList;
         var playerPrefsList = PlayerPrefsList;

         for (var i = pmPrefsList.Count - 1; i >= 0; i--)
         {
            var pref = pmPrefsList[i];

            if (pref.DeleteMarker)
            {
               PmPrefs.DeleteKey(pref.Key);
               PmPrefsList.RemoveAt(i);
               continue;
            }

            if (pref.Changed)
               PmPrefs.Save(pref.Key, pref.Value);
         }

         for (var i = playerPrefsList.Count - 1; i >= 0; i--)
         {
            var pref = playerPrefsList[i];

            if (pref.DeleteMarker)
            {
               PlayerPrefs.DeleteKey(pref.Key);
               PlayerPrefsList.RemoveAt(i);
               continue;
            }

            if (pref.Changed)
            {
               bool intParsed = int.TryParse(pref.Value, out var iParse);
               bool floatParsed = float.TryParse(pref.Value, out var fParse);

               if (intParsed)
                  PlayerPrefs.SetInt(pref.Key, iParse);
               else if (floatParsed)
                  PlayerPrefs.SetFloat(pref.Key, fParse);
               else
                  PlayerPrefs.SetString(pref.Key, pref.Value);
            }
         }

         Initialize();
      }

      private void GetKeys()
      {
         PmPrefsList = new List<PmPrefsListItem>();
         PlayerPrefsList = new List<PmPrefsListItem>();

         GetWindowsKeys.GetKeys();

         if (PmPrefsList.Count > 0)
            FillList(_listViewPmPrefsList, PmPrefsList);

         if (PlayerPrefsList.Count > 0)
            FillList(_listViewPlayerPrefsList, PlayerPrefsList);
      }

      private void ChangeSecureKey()
      {
         var key = _changeSecureKeyField.value;

         if (key.Length < 8)
         {
            EditorUtility.DisplayDialog("Error", "Key must be at least 8 characters long", "OK");
            return;
         }

         if (!Regex.IsMatch(key, @"^[a-zA-Z0-9][\w]*$"))
         {
            EditorUtility.DisplayDialog("Error", "Key must be alphanumeric", "OK");
            return;
         }

         if (EditorUtility.DisplayDialog("Change Key", "Are you sure you want to change the secure key?\nAll PmPrefs will be deleted!", "Yes", "No"))
         {
            PmPrefs.DeleteAll();
            PmPrefs.SaveAll();

            PmPrefsList.Clear();
            PlayerPrefsList.Clear();

            _listViewPmPrefsList.Clear();
            _listViewPlayerPrefsList.Clear();

            var file = FindFile("PmPrefs.cs", Application.dataPath);
            string[] readText = File.ReadAllLines(file);

            for (var i = 0; i < readText.Length; i++)
            {
               string s = readText[i];
               if (s.Contains("private const string SecureKey ="))
               {
                  string toReplace = Regex.Match(s, "\"([^\"]*)\"").Groups[1].Value;
                  string correctString = s.Replace(toReplace, key);
                  readText[i] = correctString;

                  File.WriteAllLines(file, readText);
                  return;
               }
            }

            Initialize();
         }
      }

      private string FindFile(string filename, string folder)
      {
         var files = Directory.GetFiles(folder, filename);

         if (files.Length > 0)
            return files[0];

         var dirs = Directory.GetDirectories(folder);

         foreach (var dir in dirs)
         {
            var file = FindFile(filename, dir);
            if (file != null)
               return file;
         }

         return null;
      }
   }
}
#endif