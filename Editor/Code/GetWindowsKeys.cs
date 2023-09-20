#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using UnityEditor;
using UnityEngine;

namespace PM.Plugins
{
   public class GetWindowsKeys
   {
      private PmPrefsEditorWindow _pmPrefsEditorWindow;

      public GetWindowsKeys(PmPrefsEditorWindow pmPrefsEditorWindow)
      {
         _pmPrefsEditorWindow = pmPrefsEditorWindow;
      }

      internal void GetKeys()
      {
         var key = Registry.CurrentUser.OpenSubKey(@"Software\Unity\UnityEditor\" + PlayerSettings.companyName + @"\" + PlayerSettings.productName);

         if (key == null) return;

         var list = key.GetValueNames().ToList();
         list.Sort();

         foreach (var subKey in list)
         {
            var keyName = subKey[..subKey.LastIndexOf("_", StringComparison.Ordinal)];

            if (keyName == "PmPrefs_KeyList") continue;

            var val = key.GetValue(subKey);

            if (val is not int && val is not float)
               val = Encoding.ASCII.GetString((byte[])val);

            var str = val.ToString();

            if (keyName.StartsWith(PmPrefsEditorWindow.Prefix))
            {
               var loadingKey = keyName.Replace(PmPrefsEditorWindow.Prefix, string.Empty);
       
               if (_pmPrefsEditorWindow.ShowEncrypted)
                  str = PmPrefs.Decrypt(PlayerPrefs.GetString(keyName));
               else
                  str = PlayerPrefs.GetString(keyName);

               _pmPrefsEditorWindow.PmPrefsList.Add(new PmPrefsListItem(loadingKey, str));
            }
            else
            {
               _pmPrefsEditorWindow.PlayerPrefsList.Add(new PmPrefsListItem(keyName, str));
            }
         }
      }
   }
}
#endif