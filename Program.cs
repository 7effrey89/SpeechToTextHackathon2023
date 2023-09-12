using System.Globalization;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.CognitiveServices.Speech.Translation;

namespace SpeechToText
{
    internal class Program
    {
        // This example requires environment variables named "SPEECH_KEY" and "SPEECH_REGION"
        static string speechKey = "9f76706cb0b74dfeb7b120508bd71966";
        static string speechRegion = "swedencentral";

        static string customEndpoint_En_Light = "4fd1dac9-b594-483e-ab6f-e0d3c0c71a75";

        static string customEndpoint_Dk_Light = "eb0d964b-d172-4d69-911c-53ab56f95766";
        
        static string customEndpoint_In_Full = "a48f54f4-05b5-4fe9-8fb7-66c40bdfb564";
        static string customEndpoint_Dk_Full = "30b18489-b8e3-44ad-8168-0fee6f380a11";

        /*
         * Custom pronounciation
         https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-custom-speech-test-and-train#pronunciation-data-for-training
        https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-custom-speech-human-labeled-transcriptions
         */
        static public bool useCustomEndpoint = true; //will always use the language set in the endpoint

        /*
         * Using PhraseList as alternative /supplement to improve accuracy on recognizing  
         * Just-in-time: A phrase list is provided just before starting the speech recognition, eliminating the need to train a custom model
         * Csonideration:There are some situations where training a custom model that includes phrases is likely the best option to improve accuracy. For example, in the following cases you would use Custom Speech:
         *  If you need to use a large list of phrases. A phrase list shouldn't have more than 500 phrases.
         *  If you need a phrase list for languages that are not currently supported.
         https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-custom-speech-test-and-train#pronunciation-data-for-training
         */
        static public bool usePhraseList = false;

        /*
        * https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-identification?tabs=once&pivots=programming-language-csharp
        */
        static public bool useAutoLanguageDetection = true; //not in scope


        static public bool usePredefinedAUdioFile = false;
        static public bool useTranslation = true;

        async static Task Main(string[] args)
        {
            var speechConfig = SpeechTranslationConfig.FromSubscription(speechKey, speechRegion);
            //await FromMic(speechConfig);

            await SpeechRegClass(speechConfig);
            //await TranscriberClass(speechConfig);
        }


        async static Task SpeechRegClass(SpeechTranslationConfig speechConfig)
        {
            Console.WriteLine("Say something... press any key to stop");

            //source: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/get-started-stt-diarization?tabs=windows&pivots=programming-language-csharp
            //source: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-recognize-speech?pivots=programming-language-csharp#change-how-silence-is-handled

            //Language: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support?tabs=stt
            //speechConfig.SpeechRecognitionLanguage = "en-US";
            //speechConfig.SpeechRecognitionLanguage = "ml-IN";
            //speechConfig.SpeechRecognitionLanguage = "da-DK";
            //speechConfig.SpeechRecognitionLanguage = "ru-RU";

            speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "500"); //500 ms default

            //init auto detect language
            AutoDetectSourceLanguageConfig autoDetectSourceLanguageConfig = AutoDetectSourceLanguageConfig.FromLanguages(new string[] { "en-US"});


            if (useCustomEndpoint)
            {
                
                if (useAutoLanguageDetection)
                {
                    Console.WriteLine("The first sentence will determine which language to use.");
                    //When using auto detect language, you need to specify which endpoint to use for each language as each endpoint support only 1 specific language
                    var sourceLanguageConfigs = new SourceLanguageConfig[]
                    {
                        SourceLanguageConfig.FromLanguage("en-US", customEndpoint_En_Light),
                        SourceLanguageConfig.FromLanguage("da-DK", customEndpoint_Dk_Full),
                        SourceLanguageConfig.FromLanguage("ml-IN", customEndpoint_In_Full)
                    };
                    autoDetectSourceLanguageConfig =
                        AutoDetectSourceLanguageConfig.FromSourceLanguageConfigs(
                            sourceLanguageConfigs);

                } else
                {
                    //Hard coding which custom endpoint to use based on language
                    switch (speechConfig.SpeechRecognitionLanguage)
                    {
                        case "en-US":
                            speechConfig.EndpointId = customEndpoint_En_Light;
                            break;
                        case "da-DK":
                            speechConfig.EndpointId = customEndpoint_Dk_Full; //change to danish endpoint

                            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(speechConfig.SpeechRecognitionLanguage);
                            break;
                        case "ml-IN":
                            speechConfig.EndpointId = customEndpoint_In_Full; //change to danish endpoint
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(speechConfig.SpeechRecognitionLanguage);
                            break;
                        default:
                            speechConfig.EndpointId = null;
                            break;
                    }
                }
            } else
            {
                //when not using custom endpoint, auto selection can be deone like this:
                autoDetectSourceLanguageConfig =
                AutoDetectSourceLanguageConfig.FromLanguages(
                    new string[] { "en-US", "ml-IN", "ru-RU", "da-DK" });
            }

            var stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            AudioConfig myVariableAudioConfig = AudioConfig.FromDefaultMicrophoneInput();
            if (usePredefinedAUdioFile)
            {
                var filepath = "C:\\Git\\SpeechToText\\SpeechToText\\Assets\\Recording.wav";
                myVariableAudioConfig = AudioConfig.FromWavFileInput(filepath);
            }


            // Create an audio stream from a wav file or from the default microphone
            using (var audioConfig = myVariableAudioConfig)
            {
                //default use speechConfig for language selection
                SpeechRecognizer myVariableRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

                List<string> transcriptedLines = new List<string>();

                //option to auto select language 
                if (useAutoLanguageDetection)
                {
                    //auto detect language
                    myVariableRecognizer = new SpeechRecognizer(speechConfig, autoDetectSourceLanguageConfig, audioConfig); 
                }
                // Create a conversation transcriber using audio stream input
                using (var conversationTranscriber = myVariableRecognizer)
                {

                    if (usePhraseList)
                    {
                        PhraseListGrammar phraseList = PhraseListGrammar.FromRecognizer(conversationTranscriber);
                        phraseList.AddPhrase("CSAM");
                        phraseList.AddPhrase("CAF");
                        phraseList.AddPhrase("WAF");
                        phraseList.AddPhrase("ECIF");
                    }

                    conversationTranscriber.Recognizing += (s, e) =>
                    {
                        //Console.WriteLine($"TRANSCRIBING: Text={e.Result.Text}");
                    };

                    conversationTranscriber.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            var autoDetectSourceLanguageResult =
                            AutoDetectSourceLanguageResult.FromResult(e.Result);
                            string detectedLanguage = autoDetectSourceLanguageResult.Language;

                            string outcomeText= $"RECOGNIZED Language: {detectedLanguage}: Text={e.Result.Text}";
                            Console.WriteLine(outcomeText);
                            transcriptedLines.Add(outcomeText);
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            Console.WriteLine($"NOMATCH: Speech could not be transcribed.");
                        }
                    };

                    conversationTranscriber.Canceled += (s, e) =>
                    {
                        Console.WriteLine($"CANCELED: Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                            stopRecognition.TrySetResult(0);
                        }

                        stopRecognition.TrySetResult(0);
                    };

                    conversationTranscriber.SessionStopped += (s, e) =>
                    {
                        Console.WriteLine("\n    Session stopped event.");
                        stopRecognition.TrySetResult(0);

                        writeToFile(transcriptedLines);
                    };


                    await myVariableRecognizer.StartContinuousRecognitionAsync();

                    Console.ReadKey();
                    //Related to auto language detection
                    if (useAutoLanguageDetection)
                    {
                        var speechRecognitionResult = await conversationTranscriber.RecognizeOnceAsync();
                        var autoDetectSourceLanguageResult =
                            AutoDetectSourceLanguageResult.FromResult(speechRecognitionResult);
                        var detectedLanguage = autoDetectSourceLanguageResult.Language;
                    }

                    // Waits for completion. Use Task.WaitAny to keep the task rooted.
                    //Task.WaitAny(new[] { stopRecognition.Task });

                    //await conversationTranscriber.StopContinuousRecognitionAsync();
                    await myVariableRecognizer.StopContinuousRecognitionAsync();

                    
                }
            }

        }
        async static Task TranscriberClass(SpeechTranslationConfig speechConfig)
        {
            Console.WriteLine("Say something...");

            //source: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/get-started-stt-diarization?tabs=windows&pivots=programming-language-csharp
            //source: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-recognize-speech?pivots=programming-language-csharp#change-how-silence-is-handled

            //Language: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support?tabs=stt
            //speechConfig.SpeechRecognitionLanguage = "en-US";
            //speechConfig.SpeechRecognitionLanguage = "ml-IN";
            //speechConfig.SpeechRecognitionLanguage = "da-DK";
            //speechConfig.SpeechRecognitionLanguage = "ru-RU";


            //var autoDetectSourceLanguageConfig =
            //AutoDetectSourceLanguageConfig.FromLanguages(new string[] { "en-US", "ml-IN", "ru-RU", "da-DK" });

            speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "500"); //500 ms default

            /**/
            if (useCustomEndpoint)
            {
                speechConfig.EndpointId = customEndpoint_En_Light;

            }

            var stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            AudioConfig myVariableAudioConfig = AudioConfig.FromDefaultMicrophoneInput();
            if (usePredefinedAUdioFile)
            {
                var filepath = "C:\\Git\\SpeechToText\\SpeechToText\\Assets\\Recording.wav";
                myVariableAudioConfig = AudioConfig.FromWavFileInput(filepath);
            }


            speechConfig.SpeechRecognitionLanguage = "en-US";
            speechConfig.AddTargetLanguage("en"); //da

            // Create an audio stream from a wav file or from the default microphone
            using (var audioConfig = myVariableAudioConfig)
            {


                TranslationRecognizer myVariableRecognizer = new TranslationRecognizer(speechConfig, audioConfig);


                if (useAutoLanguageDetection)
                {
                    //auto detect language
                    // Create a SourceLanguageConfig object with the endpointId for the desired language
                    SourceLanguageConfig sourceLanguageConfig = SourceLanguageConfig.FromLanguage(customEndpoint_En_Light);

                    // Create an AutoDetectSourceLanguageConfig object using the SourceLanguageConfig object
                    AutoDetectSourceLanguageConfig autoDetectSourceLanguageConfig = AutoDetectSourceLanguageConfig.FromLanguages(new string[] { "en-US", "ml-IN", "ru-RU", "da-DK" });

                    myVariableRecognizer = new TranslationRecognizer(speechConfig, autoDetectSourceLanguageConfig, audioConfig); //fails here for custom endpoint
                }
                // Create a conversation transcriber using audio stream input
                using (var conversationTranscriber = myVariableRecognizer)
                {

                    if (usePhraseList)
                    {
                        PhraseListGrammar phraseList = PhraseListGrammar.FromRecognizer(conversationTranscriber);
                        phraseList.AddPhrase("CSAM");
                        phraseList.AddPhrase("CAF");
                        phraseList.AddPhrase("WAF");
                        phraseList.AddPhrase("ECIF");
                    }

                    conversationTranscriber.Recognizing += (s, e) =>
                    {
                        Console.WriteLine($"TRANSCRIBING: Text={e.Result.Text}");
                    };

                    conversationTranscriber.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.TranslatedSpeech)
                        {
                            Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");

                            foreach (var element in e.Result.Translations)
                            {
                                Console.WriteLine($"TRANSLATED into '{element.Key}': {element.Value}");
                            }
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            Console.WriteLine($"NOMATCH: Speech could not be transcribed.");
                        }
                    };

                    conversationTranscriber.Canceled += (s, e) =>
                    {
                        Console.WriteLine($"CANCELED: Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                            stopRecognition.TrySetResult(0);
                        }

                        stopRecognition.TrySetResult(0);
                    };

                    conversationTranscriber.SessionStopped += (s, e) =>
                    {
                        Console.WriteLine("\n    Session stopped event.");
                        stopRecognition.TrySetResult(0);
                    };

                    //await conversationTranscriber.StartContinuousRecognitionAsync();

                    await myVariableRecognizer.StartContinuousRecognitionAsync();

                    //Related to auto language detection
                    //if (useAutoLanguageDetection)
                    //{
                    //    var speechRecognitionResult = await conversationTranscriber.RecognizeOnceAsync();
                    //    var autoDetectSourceLanguageResult =
                    //        AutoDetectSourceLanguageResult.FromResult(speechRecognitionResult);
                    //    var detectedLanguage = autoDetectSourceLanguageResult.Language;
                    //}

                    // Waits for completion. Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    //await conversationTranscriber.StopContinuousRecognitionAsync();
                    await myVariableRecognizer.StopContinuousRecognitionAsync();
                }
            }

        }
        private static void writeToFile(List<string> stringCollections)
        {
            string path = @"C:\temp\output.txt";

            using (StreamWriter writer = new StreamWriter(path))
            {
                foreach (string line in stringCollections)
                {
                    writer.WriteLine(line);
                }
            }
        }
    }

}