using System;

namespace SmartNPC
{
    [Serializable]
    public class SmartNPCCharacterInfo
    {
        public string id;
        public string name;
        public string background;
        public string gender;
        public string language;
        public string[] personalityTraits;
        public string[] dialogueStyle;
        public string[] actions;
        public string[] gestures;
        public string[] expressions;
    }
}
