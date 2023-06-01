using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using static UnityEngine.UI.Scrollbar;

namespace SmartNPC
{
    public class SmartNPCScrollableText : MonoBehaviour
    {
        private TextMeshProUGUI _textField;
        private Scrollbar _scrollbar;
        
        void Awake()
        {
            _textField = GetComponentInChildren<TextMeshProUGUI>();

            if (!_textField) throw new Exception("No TextMeshProUGUI found in ScrollView");
            

            Scrollbar[] scrollbars = GetComponentsInChildren<Scrollbar>();

            for (int i = 0; i < scrollbars.Length; i++)
            {
                Scrollbar scrollbar = scrollbars[i];

                if (scrollbar.direction == Direction.BottomToTop) _scrollbar = scrollbar;
            }

            if (!_scrollbar) throw new Exception("No BottomToTop Scrollbar found");
        }

        private void ScrollToBottom()
        {
            _scrollbar.value = 0;
        }

        public string Text
        {
            get { return _textField ? _textField.text : ""; }
            
            set {
                if (!_textField || value == _textField.text) return;

                _textField.text = value;

                InvokeUtility.Invoke(this, ScrollToBottom, 0.1f);
            }
        }
    }
}