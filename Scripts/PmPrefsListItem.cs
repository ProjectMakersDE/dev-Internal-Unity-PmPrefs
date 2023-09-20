namespace PM.Plugins
{
   [System.Serializable]
   public class PmPrefsListItem
   {
      public bool DeleteMarker;
      public string Key;
      public string Value;
      private string _initial;

      public bool Changed => Value != _initial;

      public void Save() => _initial = Value;
      public void Reset() => Value = _initial;

      public PmPrefsListItem(string key, string value)
      {
         Key = key;
         _initial = Value = value;
      }
   }
}