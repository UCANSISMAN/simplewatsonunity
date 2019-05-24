using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Dialogue : MonoBehaviour {

    [System.Serializable]
    public class DialogueList {
        [DialogueDrawer(new string[] { "Soru", "Çevirisi", "Cevap 1", "Cevap 2", "Cevap 3"})]
        public string[] line = new string[5]; 
    }
    public DialogueList[] dialogue;

}



