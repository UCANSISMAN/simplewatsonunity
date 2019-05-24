#pragma warning disable 0649

using UnityEngine;
using IBM.Watson.DeveloperCloud.Services.TextToSpeech.v1;
using IBM.Watson.DeveloperCloud.Logging;
using IBM.Watson.DeveloperCloud.Utilities;
using System.Collections;
using System.Collections.Generic;
using IBM.Watson.DeveloperCloud.Connection;

public class IBM_TTS : MonoBehaviour
{
    #region PLEASE SET THESE VARIABLES IN THE INSPECTOR
    [Space(10)]
    [Tooltip("The service URL (optional). This defaults to \"https://stream.watsonplatform.net/text-to-speech/api\"")]
    [SerializeField]
    private string _serviceUrl;
    [Header("IAM Authentication")]
    [Tooltip("The IAM apikey.")]
    [SerializeField]
    private string _iamApikey;
    #endregion

    TextToSpeech _service;
    string _speechString = "<speak version=\"1.0\"><express-as type=\"GoodNews\"> Welcome to our prototype! </express-as></speak>";

    string _testWord = "Watson";

    private bool _synthesizeTested = false;
    private bool _getVoicesTested = false;
    private bool _getVoiceTested = false;
    private bool _getPronuciationTested = false;

    void Start()
    {
        LogSystem.InstallDefaultReactors();
        Runnable.Run(ServiceCreator());
    }

    private IEnumerator ServiceCreator()
    {
        if (string.IsNullOrEmpty(_iamApikey))
        {
            throw new WatsonException("Plesae provide IAM ApiKey for the service.");
        }

        //  Create credential and instantiate service
        Credentials credentials = null;

        //  Authenticate using iamApikey
        TokenOptions tokenOptions = new TokenOptions()
        {
            IamApiKey = _iamApikey
        };

        credentials = new Credentials(tokenOptions, _serviceUrl);

        //  Wait for tokendata
        while (!credentials.HasIamTokenData())
            yield return null;

        _service = new TextToSpeech(credentials);

        Runnable.Run(Examples());
    }

    public void RunTTS(string txt) {
        _speechString = "<speak version=\"1.0\"><express-as type=\"GoodNews\"> "+ txt +" </express-as></speak>";
        Runnable.Run(TextToSpeech(_speechString));
    }

    private IEnumerator TextToSpeech(string _testString)
    {
        //  Synthesize
        Log.Debug("IBM_TTS.TextToSpeech()", "Attempting synthesize.");
        _service.Voice = VoiceType.en_US_Allison;
        _service.ToSpeech(HandleToSpeechCallback, OnFail, _testString, true);
        while (!_synthesizeTested)
            yield return null;

        //	Get Voices
        Log.Debug("IBM_TTS.TextToSpeech()", "Attempting to get voices.");
        _service.GetVoices(OnGetVoices, OnFail);
        while (!_getVoicesTested)
            yield return null;

        //	Get Voice
        Log.Debug("IBM_TTS.TextToSpeech()", "Attempting to get voice {0}.", VoiceType.en_US_Allison);
        _service.GetVoice(OnGetVoice, OnFail, VoiceType.en_US_Allison);
        while (!_getVoiceTested)
            yield return null;

        Log.Debug("IBM_TTS.TextToSpeech()", "Text to Speech examples complete.");
    }

    void HandleToSpeechCallback(AudioClip clip, Dictionary<string, object> customData = null)
    {
        PlayClip(clip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (Application.isPlaying && clip != null)
        {
            GameObject audioObject = new GameObject("AudioObject");
            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.spatialBlend = 0.0f;
            source.loop = false;
            source.clip = clip;
            source.Play();

            Destroy(audioObject, clip.length);

            _synthesizeTested = true;
        }
    }

    private void OnGetVoices(Voices voices, Dictionary<string, object> customData = null)
    {
        Log.Debug("IBM_TTS.OnGetVoices()", "Text to Speech - Get voices response: {0}", customData["json"].ToString());
        _getVoicesTested = true;
    }

    private void OnGetVoice(Voice voice, Dictionary<string, object> customData = null)
    {
        Log.Debug("IBM_TTS.OnGetVoice()", "Text to Speech - Get voice  response: {0}", customData["json"].ToString());
        _getVoiceTested = true;
    }

    private void OnGetPronunciation(Pronunciation pronunciation, Dictionary<string, object> customData = null)
    {
        Log.Debug("IBM_TTS.OnGetPronunciation()", "Text to Speech - Get pronunciation response: {0}", customData["json"].ToString());
        _getPronuciationTested = true;
    }

    private void OnFail(RESTConnector.Error error, Dictionary<string, object> customData)
    {
        Log.Error("IBM_TTS.OnFail()", "Error received: {0}", error.ToString());
    }
}