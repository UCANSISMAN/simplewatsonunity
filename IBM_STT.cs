using UnityEngine;
using System.Collections;
using IBM.Watson.DeveloperCloud.Logging;
using IBM.Watson.DeveloperCloud.Services.SpeechToText.v1;
using IBM.Watson.DeveloperCloud.Utilities;
using IBM.Watson.DeveloperCloud.DataTypes;
using System.Collections.Generic;
using UnityEngine.UI;
using IBM.Watson.DeveloperCloud.Services.TextToSpeech.v1;
using IBM.Watson.DeveloperCloud.Connection;
using System;

[RequireComponent(typeof(AudioSource))]

public class IBM_STT : MonoBehaviour
{

	[Header("Speech To Text Ayarları")]
    [Tooltip("Servis URL'si. Varsayılan olarak \"https://stream.watsonplatform.net/speech-to-text/api\"")]
    [SerializeField]
    string _serviceUrl_STT;
    [Tooltip("IAM apikey.")]
    [SerializeField]
    string _iamApiKey_STT;

    int _recordingRoutine = 0;
    string _microphoneID = null;
    AudioClip _recording = null;
    AudioSource _as = null;
    int _recordingBufferSize = 1;
    int _recordingHZ = 22050;

    SpeechToText _speechToText;

    string resultString;
    [HideInInspector] public string ResultString {
        get { return resultString; }
        set {
            if (resultString == value)
                 return;
 
             resultString = value;
 
             if (resultUpdated != null)
                 resultUpdated(resultString);
        }
    }

    public delegate void OnVariableChangeDelegate(string _resultString);
    public event OnVariableChangeDelegate resultUpdated;

	Credentials credentials_STT = null;
    [HideInInspector] public Credentials Credentials_STT {
        get { return credentials_STT; }
    } 
    void Start()
    {
        _as = GetComponent<AudioSource>();
        LogSystem.InstallDefaultReactors();
		Runnable.Run(ServiceCreator());
    }

    IEnumerator ServiceCreator() {

		// Speech To Text Bağlantısı
		if (!string.IsNullOrEmpty(_iamApiKey_STT))
        {
            //  API anahtarı ile bağlan
            TokenOptions tokenOptions = new TokenOptions()
            {
                IamApiKey = _iamApiKey_STT
            };

            credentials_STT = new Credentials(tokenOptions, _serviceUrl_STT);

            //  Token gelene kadar bekle
            while (!credentials_STT.HasIamTokenData())
                yield return null;

            _speechToText = new SpeechToText(credentials_STT);
        }
        else
        {
            Debug.Log("IAM Speech To Text API anahtarını giriniz.");
        }

        
        _speechToText.StreamMultipart = true;

		Active = true;
	    StartRecording();
	}

    bool Active
    {
        get { return _speechToText.IsListening; }
        set
        {
            if (value && !_speechToText.IsListening)
            {
                _speechToText.DetectSilence = true;
                _speechToText.EnableWordConfidence = true;
                _speechToText.EnableTimestamps = true;
                _speechToText.SilenceThreshold = 0.01f;
                _speechToText.MaxAlternatives = 0;
                _speechToText.EnableInterimResults = true;
                _speechToText.OnError = OnError;
                _speechToText.InactivityTimeout = -1;
                _speechToText.ProfanityFilter = false;
                _speechToText.SmartFormatting = true;
                _speechToText.SpeakerLabels = false;
                _speechToText.WordAlternativesThreshold = null;
                _speechToText.StartListening(OnRecognize, OnRecognizeSpeaker);
            }
            else if (!value && _speechToText.IsListening)
            {
                _speechToText.StopListening();
            }
        }
    }

    private void StartRecording()
    {
        if (_recordingRoutine == 0)
        {
            UnityObjectUtil.StartDestroyQueue();
            _recordingRoutine = Runnable.Run(RecordingHandler());
        }
    }

    private void StopRecording()
    {
        if (_recordingRoutine != 0)
        {
            Microphone.End(_microphoneID);
            Runnable.Stop(_recordingRoutine);
            _recordingRoutine = 0;
        }
    }

    private void OnError(string error)
    {
        Active = false;

        Log.Debug("OnError()", "Hata! {0}", error);
    }

    private IEnumerator RecordingHandler()
    {
        Log.Debug("RecordingHandler()", "Aygıtlar: {0}", Microphone.devices);
        _recording = Microphone.Start(_microphoneID, true, _recordingBufferSize, _recordingHZ);
        yield return null;

        if (_recording == null)
        {
            StopRecording();
            yield break;
        }

        bool bFirstBlock = true;
        int midPoint = _recording.samples / 2;
        float[] samples = null;

        while (_recordingRoutine != 0 && _recording != null)
        {
            int writePos = Microphone.GetPosition(_microphoneID);
            if (writePos > _recording.samples || !Microphone.IsRecording(_microphoneID))
            {
                Log.Error("RecordingHandler()", "Mikrofon bağlantısı kesildi.");

                StopRecording();
                yield break;
            }

            if ((bFirstBlock && writePos >= midPoint)
              || (!bFirstBlock && writePos < midPoint))
            {
                samples = new float[midPoint];
                _recording.GetData(samples, bFirstBlock ? 0 : midPoint);

                AudioData record = new AudioData();
				record.MaxLevel = Mathf.Max(Mathf.Abs(Mathf.Min(samples)), Mathf.Max(samples));
                record.Clip = AudioClip.Create("Kaydediliyor", midPoint, _recording.channels, _recordingHZ, false);
                record.Clip.SetData(samples, 0);

                _speechToText.OnListen(record);

                bFirstBlock = !bFirstBlock;
            }
            else
            {
                int remaining = bFirstBlock ? (midPoint - writePos) : (_recording.samples - writePos);
                float timeRemaining = (float)remaining / (float)_recordingHZ;

                yield return new WaitForSeconds(timeRemaining);
            }

        }

        yield break;
    }

    private void OnRecognize(SpeechRecognitionEvent result, Dictionary<string, object> customData)
    {
        if (result != null && result.results.Length > 0)
        {
            foreach (var res in result.results)
            {
                foreach (var alt in res.alternatives)
                {
                    string text = string.Format("{0} ({1}, {2:0.00})\n", alt.transcript, res.final ? "Final" : "Interim", alt.confidence);
                    Log.Debug("OnRecognize()", text);
                    if (text.Contains("Final")) {
                        ResultString = alt.transcript;
                    }
                }

                if (res.keywords_result != null && res.keywords_result.keyword != null)
                {
                    foreach (var keyword in res.keywords_result.keyword)
                    {
                        Log.Debug("OnRecognize()", "keyword: {0}, confidence: {1}, start time: {2}, end time: {3}", keyword.normalized_text, keyword.confidence, keyword.start_time, keyword.end_time);
                    }
                }

                if (res.word_alternatives != null)
                {
                    foreach (var wordAlternative in res.word_alternatives)
                    {
                        Log.Debug("OnRecognize()", "Word alternatives found. Start time: {0} | EndTime: {1}", wordAlternative.start_time, wordAlternative.end_time);
                        foreach(var alternative in wordAlternative.alternatives)
                            Log.Debug("OnRecognize()", "\t word: {0} | confidence: {1}", alternative.word, alternative.confidence);
                    }
                }
            }
        }
    }

    private void OnRecognizeSpeaker(SpeakerRecognitionEvent result, Dictionary<string, object> customData)
    {
        if (result != null)
        {
            foreach (SpeakerLabelsResult labelResult in result.speaker_labels)
            {
                Log.Debug("OnRecognize()", string.Format("speaker result: {0} | confidence: {3} | from: {1} | to: {2}", labelResult.speaker, labelResult.from, labelResult.to, labelResult.confidence));
                
            }
        }
    }

}
