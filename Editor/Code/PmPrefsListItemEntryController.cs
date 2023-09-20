using UnityEngine;
using UnityEngine.UIElements;

namespace PM.Plugins
{
   public class PmPrefsListItemEntryController
   {
      private Label _keyLabel;
      private TextField _valueLabel;
      private Toggle _deleteToggle;
      private bool _isChanged;

      private PmPrefsListItem _data;
      private VisualElement _root;

      public void SetVisualElement(VisualElement visualElement)
      {
         _root = visualElement;
         _keyLabel = visualElement.Q<Label>("Item_name");
         _valueLabel = visualElement.Q<TextField>("Item_value");
         _deleteToggle = visualElement.Q<Toggle>("Item_delete");
      }

      public string GetValue() => _valueLabel.value;

      public bool GetDelete() => _deleteToggle.value;

      public string GetKey() => _keyLabel.text;
      public bool GetChanged() => _isChanged;

      public void SetVisibility(bool b)
      {
         _root.style.display = b ? DisplayStyle.Flex : DisplayStyle.None;
      }

      public void SetData(PmPrefsListItem data)
      {
         _data = data;
         Initialize();
      }

      private void Initialize()
      {
         _keyLabel.text = _data.Key;
         _valueLabel.SetValueWithoutNotify(_data.Value);
         _deleteToggle.SetValueWithoutNotify(_data.DeleteMarker);

         _valueLabel.RegisterValueChangedCallback(OnValueChanged);
         _deleteToggle.RegisterValueChangedCallback(OnDeleteChanged);
      }

      private void OnDeleteChanged(ChangeEvent<bool> evt)
      {
         _data.DeleteMarker = evt.newValue;

         if (_data.DeleteMarker)
         {
            _valueLabel.style.borderBottomWidth = 2;
            _valueLabel.style.borderLeftWidth = 2;
            _valueLabel.style.borderRightWidth = 2;
            _valueLabel.style.borderTopWidth = 2;
            _valueLabel.style.borderBottomColor = new Color(0.6f, 0.27f, 0.27f);
            _valueLabel.style.borderLeftColor = new Color(0.6f, 0.27f, 0.27f);
            _valueLabel.style.borderRightColor = new Color(0.6f, 0.27f, 0.27f);
            _valueLabel.style.borderTopColor = new Color(0.6f, 0.27f, 0.27f);
         }
         else
         {
            _valueLabel.style.borderBottomWidth = 0;
            _valueLabel.style.borderLeftWidth = 0;
            _valueLabel.style.borderRightWidth = 0;
            _valueLabel.style.borderTopWidth = 0;

            _valueLabel.style.borderBottomColor = new StyleColor(new Color32(0, 0, 0, 0));
            _valueLabel.style.borderLeftColor = new StyleColor(new Color32(0, 0, 0, 0));
            _valueLabel.style.borderRightColor = new StyleColor(new Color32(0, 0, 0, 0));
            _valueLabel.style.borderTopColor = new StyleColor(new Color32(0, 0, 0, 0));
         }
      }

      private void OnValueChanged(ChangeEvent<string> evt)
      {
         _data.Value = evt.newValue;
         _isChanged = true;

         _valueLabel.style.borderBottomWidth = 2;
         _valueLabel.style.borderLeftWidth = 2;
         _valueLabel.style.borderRightWidth = 2;
         _valueLabel.style.borderTopWidth = 2;
         _valueLabel.style.borderBottomColor = new Color(0.35f, 0.61f, 0.3f);
         _valueLabel.style.borderLeftColor = new Color(0.35f, 0.61f, 0.3f);
         _valueLabel.style.borderRightColor = new Color(0.35f, 0.61f, 0.3f);
         _valueLabel.style.borderTopColor = new Color(0.35f, 0.61f, 0.3f);
      }
   }
}