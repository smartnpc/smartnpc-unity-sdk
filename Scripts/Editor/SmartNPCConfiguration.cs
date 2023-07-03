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
        public static void SetupSmartNPCAPIKey()
        {
            SmartNPCConfiguration window = GetWindow<SmartNPCConfiguration>();
            
            window.titleContent = new GUIContent("Configuration");

            window.minSize = window.maxSize = new Vector2(600, 670);
        }
        

        private delegate T GetHandler<T>();
        private delegate void SetHandler<T>(T value);

        private SmartNPCConnectionConfig config;

        private SmartNPCConnectionConfig GetOrCreateConfig()
        {
            string path = "Assets/SmartNPC/Resources/SmartNPC Connection Config.asset";

            SmartNPCConnectionConfig result = AssetDatabase.LoadAssetAtPath<SmartNPCConnectionConfig>(path);

            if (!result)
            {
                result = CreateInstance<SmartNPCConnectionConfig>();

                if (!AssetDatabase.IsValidFolder("Assets/SmartNPC")) AssetDatabase.CreateFolder("Assets", "SmartNPC");
                if (!AssetDatabase.IsValidFolder("Assets/SmartNPC/Resources")) AssetDatabase.CreateFolder("Assets/SmartNPC", "Resources");

                AssetDatabase.CreateAsset(result, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return result;
        }

        public void OnEnable()
        {
            config = GetOrCreateConfig();
        }

        private Image GetLogo()
        {
            Image logo = new Image();

            logo.image = AssetDatabase.LoadAssetAtPath<Texture>("Assets/SmartNPC/Images/logo.png");

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

        private Label GetHeader(string text)
        {
            Label result = new Label(text);

            result.style.fontSize = 14;
            result.style.unityFontStyleAndWeight = FontStyle.Bold;
            result.style.marginBottom = 10;
            result.style.color = Color.white;

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

            Label header = GetHeader("API Key");

            TextField keyId = GetTextField("Key ID", () => config.APIKey.KeyId, (string value) => config.APIKey.KeyId = value);
            TextField publicKey = GetTextField("Public Key", () => config.APIKey.PublicKey, (string value) => config.APIKey.PublicKey = value);

            Button studio = GetButton("Get an API Key", () =>
            {
                Application.OpenURL("https://studio.smartnpc.ai");
            });

            studio.style.marginTop = 10;

            result.Add(header);
            result.Add(keyId);
            result.Add(publicKey);
            result.Add(studio);

            return result;
        }

        private VisualElement GetPlayer()
        {
            VisualElement result = new VisualElement();

            result.style.flexDirection = FlexDirection.Column;

            Label header = GetHeader("Player");

            TextField playerId = GetTextField("ID", () => config.Player.Id, (string value) => config.Player.Id = value);
            TextField playerName = GetTextField("Name", () => config.Player.Name, (string value) => config.Player.Name = value);

            result.Add(header);
            result.Add(playerId);
            result.Add(playerName);

            return result;
        }

        private VisualElement GetVoice()
        {
            VisualElement result = new VisualElement();

            result.style.flexDirection = FlexDirection.Column;

            Label header = GetHeader("Voice");


            Toggle voiceEnabled = GetToggle("Enabled", () => config.Voice.Enabled, (bool value) => config.Voice.Enabled = value);

            Slider voiceVolume = new Slider("Volume", 0, 1);

            voiceVolume.value = config.Voice.Volume;

            voiceVolume.RegisterValueChangedCallback((evt) => config.Voice.Volume = evt.newValue);


            IntegerField voiceMaxDistance = new IntegerField("Max Distance");

            voiceMaxDistance.value = config.Voice.MaxDistance;

            voiceMaxDistance.RegisterValueChangedCallback((evt) => config.Voice.MaxDistance = evt.newValue);

            result.Add(header);
            result.Add(voiceEnabled);
            result.Add(voiceVolume);
            result.Add(voiceMaxDistance);

            return result;
        }

        private VisualElement GetBehaviors()
        {
            VisualElement result = new VisualElement();

            result.style.flexDirection = FlexDirection.Column;

            Label header = GetHeader("Behaviors");

            Toggle behaviorsEnabled = GetToggle("Enabled", () => config.Behaviors.Enabled, (bool value) => config.Behaviors.Enabled = value);

            result.Add(header);
            result.Add(behaviorsEnabled);

            return result;
        }

        private VisualElement GetAdvanced()
        {
            VisualElement result = new VisualElement();

            result.style.flexDirection = FlexDirection.Column;

            Label header = GetHeader("Advanced");

            TextField host = GetTextField("Host", () => config.Advanced.Host, (string value) => config.Advanced.Host = value);

            result.Add(header);
            result.Add(host);

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
                Application.OpenURL("https://docs.smartnpc.ai/unity-sdk/getting-started/context");
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
                GetPlayer(),
                GetVoice(),
                GetBehaviors(),
                GetAdvanced(),
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