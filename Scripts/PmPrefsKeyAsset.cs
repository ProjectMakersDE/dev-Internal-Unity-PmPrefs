using System.Collections.Generic;
using UnityEngine;

namespace PM.Plugins
{
   [CreateAssetMenu(fileName = "PmPrefsKeyAsset", menuName = "ScriptableObject/PmPrefsKeyAsset")]
   public class PmPrefsKeyAsset : ScriptableObject
   {
      public List<PmPrefsListItem> Items = new List<PmPrefsListItem>();
   }
}