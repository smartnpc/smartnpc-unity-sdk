#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using System;

namespace SmartNPC
{
    [InitializeOnLoad]
    public class SmartNPCConfiguration : EditorWindow
    {
        [MenuItem("SmartNPC/Configuration", priority = 1)]
        public static void OpenConfiguration()
        {
            SmartNPCConfiguration window = GetWindow<SmartNPCConfiguration>();
            
            window.titleContent = new GUIContent("Configuration");

            window.minSize = window.maxSize = new Vector2(600, 660);
        }
        

        private delegate T GetHandler<T>();
        private delegate void SetHandler<T>(T value);

        private SmartNPCConnectionConfig config;
        
        public void OnEnable()
        {
            config = AssetDatabase.LoadAssetAtPath<SmartNPCConnectionConfig>(SmartNPCBootstrap.AssetsPath + "/" + SmartNPCBootstrap.ConfigRelativePath);
        }

        private Image GetLogo()
        {
            Image logo = new Image();

            logo.image = AssetDatabase.LoadAssetAtPath<Texture>(SmartNPCBootstrap.AssetsPath + "/" + SmartNPCBootstrap.LogoRelativePath);

            logo.style.paddingBottom = 10;
            logo.style.paddingTop = 20;
            logo.style.paddingRight = 10;
            logo.style.paddingLeft = 10;

            return logo;
        }

        private VisualElement GetLogoContainer()
        {
            VisualElement result = new VisualElement();

            result.style.backgroundColor = Color.white;
            result.style.paddingLeft = 10;
            result.style.paddingRight = 10;
            result.style.paddingTop = 10;
            result.style.paddingBottom = 10;
            result.style.marginBottom = 10;

            result.Add( GetLogo() );

            return result;
        }

        private Label GetHeader(string text, int margin = 10)
        {
            Label result = new Label(text);

            result.style.fontSize = 14;
            result.style.unityFontStyleAndWeight = FontStyle.Bold;
            result.style.color = Color.white;
            result.style.marginBottom = margin;

            return result;
        }

        private Label GetInstructions(string text)
        {
            Label result = new Label(text);

            result.style.fontSize = 12;
            result.style.marginBottom = 10;
            result.style.color = Color.grey;

            return result;
        }

        private TextField GetTextField(string label, GetHandler<string> getHandler, SetHandler<string> setHandler)
        {
            TextField result = new TextField(label);

            result.value = getHandler();

            result.RegisterValueChangedCallback((evt) => setHandler(evt.newValue));

            return result;
        }

        private Toggle GetToggle(string label, GetHandler<bool> getHandler, SetHandler<bool> setHandler)
        {
            Toggle result = new Toggle(label);

            result.value = getHandler();

            result.RegisterValueChangedCallback((evt) => setHandler(evt.newValue));

            return result;
        }

        private VisualElement GetCredentials()
        {
            VisualElement result = new VisualElement();

            result.style.flexDirection = FlexDirection.Column;

            TextField keyId = GetTextField("Key ID", () => config.APIKey.KeyId, (string value) => config.APIKey.KeyId = value);
            TextField publicKey = GetTextField("Public Key", () => config.APIKey.PublicKey, (string value) => config.APIKey.PublicKey = value);

            Button studio = GetButton("Get an API Key", () =>
            {
                Application.OpenURL("https://studio.smartnpc.ai");
            });

            studio.style.marginTop = 20;

            result.Add( GetHeader("API Key") );
            result.Add(keyId);
            result.Add(publicKey);
            result.Add(studio);

            return result;
        }

        private VisualElement GetUser()
        {
            VisualElement result = new VisualElement();

            result.style.flexDirection = FlexDirection.Column;

            TextField userId = GetTextField("ID", () => config.User.Id, (string value) => config.User.Id = value);
            TextField userName = GetTextField("Name", () => config.User.Name, (string value) => config.User.Name = value);

            result.Add( GetHeader("User", 5) );
            result.Add( GetInstructions("It should reflect the data you have on your end user. You can also set this at runtime instead.") );
            result.Add(userId);
            result.Add(userName);

            return result;
        }

        private VisualElement GetVoice()
        {
            VisualElement result = new VisualElement();

            result.style.flexDirection = FlexDirection.Column;

            Toggle voiceEnabled = GetToggle("Enabled", () => config.Voice.Enabled, (bool value) => config.Voice.Enabled = value);

            Slider voiceVolume = new Slider("Volume", 0, 1);

            voiceVolume.value = config.Voice.Volume;

            voiceVolume.RegisterValueChangedCallback((evt) => config.Voice.Volume = evt.newValue);


            IntegerField voiceMaxDistance = new IntegerField("Max Distance");

            voiceMaxDistance.value = config.Voice.MaxDistance;

            voiceMaxDistance.RegisterValueChangedCallback((evt) => config.Voice.MaxDistance = evt.newValue);

            result.Add(GetHeader("Voice") );
            result.Add(voiceEnabled);
            result.Add(voiceVolume);
            result.Add(voiceMaxDistance);

            return result;
        }

        private VisualElement GetBehaviors()
        {
            VisualElement result = new VisualElement();

            result.style.flexDirection = FlexDirection.Column;

            Toggle behaviorsEnabled = GetToggle("Enabled", () => config.Behaviors.Enabled, (bool value) => config.Behaviors.Enabled = value);

            result.Add( GetHeader("Behaviors", 5) );
            result.Add( GetInstructions("Actions, Gestures, and Facial Expressions") );
            result.Add(behaviorsEnabled);

            return result;
        }

        private VisualElement GetSupport()
        {
            VisualElement result = new VisualElement();

            result.style.flexDirection = FlexDirection.Column;

            Label header = GetHeader("Support");




            VisualElement buttons = new VisualElement();

            buttons.style.flexDirection = FlexDirection.Row;


            Button documentation = GetButton("Documentation", () =>
            {
                Application.OpenURL("https://docs.smartnpc.ai/unity-sdk/getting-started/configuration");
            });

            Button discord = GetButton("Discord", () =>
            {
                Application.OpenURL("https://discord.gg/GD73ZDeYzh");
            });


            buttons.Add(documentation);
            buttons.Add(discord);



            result.Add(header);
            result.Add(buttons);

            return result;
        }

        private Button GetButton(string label, Action onClick)
        {
            Button result = new Button(onClick);

            result.text = label;

            result.style.alignSelf = Align.Center;

            result.style.paddingBottom = 10;
            result.style.paddingLeft = 10;
            result.style.paddingRight = 10;
            result.style.paddingTop = 10;

            return result;
        }

        private VisualElement SpaceVerticalContent(VisualElement[] elements, int space)
        {
            VisualElement result = new VisualElement();

            result.style.flexDirection = FlexDirection.Column;

            for (int i = 0; i < elements.Length; i++)
            {
                VisualElement element = elements[i];

                if (i < elements.Length - 1) element.style.paddingBottom = space;

                result.Add(element);
            }

            return result;
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            VisualElement content = new ScrollView();

            content.style.marginBottom = 10;
            content.style.marginLeft = 10;
            content.style.marginRight = 10;
            content.style.marginTop = 10;

            VisualElement blocks = SpaceVerticalContent(new VisualElement[] {
                GetCredentials(),
                GetUser(),
                GetVoice(),
                GetBehaviors(),
                GetSupport()
            }, 20);

            content.Add(blocks);
            
            root.Add( GetLogoContainer() );
            root.Add(content);
        }

        private void OnDestroy()
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}

#endif