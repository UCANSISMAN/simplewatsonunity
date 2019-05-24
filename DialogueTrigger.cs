using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueTrigger : MonoBehaviour
{
    // soru ve cevaplarinin metinlerini editorden ayarla
    [Header("GUI Elements For Conversation")]
    public Dialogue _main;
    public Text resultText, questionText, translationText, answerText1, answerText2, answerText3;

    // THIS
    int sceneIndex = 0;

    string userResult;
    string speechString;
    AudioClip _ac;
    

    void Start()
    {
        _main = GameObject.FindWithTag("DialogueObjects").GetComponent<Dialogue>();
        _ac = GetComponent<AudioClip>();
        SetScene(sceneIndex);
    }
    
    void OnRecognize(string _resultString) {
        
        string a1 = RemoveAllPunctuations (answerText1.text);
        string a2 = RemoveAllPunctuations (answerText2.text);
        string a3 = RemoveAllPunctuations (answerText3.text);
        userResult = RemoveAllPunctuations (_resultString);
        bool goNext = false;

        if (userResult.Contains (a1)) {
            goNext = true;
        } else if (userResult.Contains (a2)) {
            goNext = true;
        } else if (userResult.Contains (a3)) {
            goNext = true;
        }

        if (goNext) {
            resultText.text = userResult;
            NextLine();
            goNext = false;
        }
        
    }

    //metindeki noktalama isaretlerini kaldir
    private string RemoveAllPunctuations (string _mm) {
        string output = _mm;
        var charsToRemove = new string[] { ",", ".", "?", "'", "!", "’" };

        foreach (var c in charsToRemove) {
            output = output.Replace (c, string.Empty);
        }

        output = output.ToLower ();
        output = output.Trim();

        return output;
    }

    void NextLine() {
        sceneIndex++;
        SetScene(sceneIndex);
    }

    void SetScene(int index) {
        if(sceneIndex <= _main.dialogue.Length) {  
            questionText.text = _main.dialogue[index].line[0];
            translationText.text = _main.dialogue[index].line[1];
            answerText1.text = _main.dialogue[index].line[2];
            answerText2.text = _main.dialogue[index].line[3];
            answerText3.text = _main.dialogue[index].line[4];
            //GetComponent<ExampleTextToSpeech>().RunTTS(questionText.text);
        }
        else {
            EndScene();
        }
        
    }

    void EndScene() {
        resultText.text = "Scene Ending";
    }
}
