using System;
using UnityEngine;

namespace SmartNPC
{
    [Serializable]
    public class SmartNPCVisemes
    {
        [SerializeField] public string sil = "viseme_sil";
        [SerializeField] public string PP = "viseme_PP";
        [SerializeField] public string FF = "viseme_FF";
        [SerializeField] public string TH = "viseme_TH";
        [SerializeField] public string DD = "viseme_DD";
        [SerializeField] public string kk = "viseme_kk";
        [SerializeField] public string CH = "viseme_CH";
        [SerializeField] public string SS = "viseme_SS";
        [SerializeField] public string nn = "viseme_nn";
        [SerializeField] public string RR = "viseme_RR";
        [SerializeField] public string aa = "viseme_aa";
        [SerializeField] public string E = "viseme_E";
        [SerializeField] public string I = "viseme_I";
        [SerializeField] public string O = "viseme_O";
        [SerializeField] public string U = "viseme_U";

        public string[] GetBlendShapes()
        {
            string[] result = {
                sil,
                PP,
                FF,
                TH,
                DD,
                kk,
                CH,
                SS,
                nn,
                RR,
                aa,
                E,
                I,
                O,
                U,
            };

            return result;
        }
    }
}
