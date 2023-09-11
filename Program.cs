using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;

namespace SpeechToText
{
    internal class Program
    {
        // This example requires environment variables named "SPEECH_KEY" and "SPEECH_REGION"
        static string speechKey = "9f76706cb0b74dfeb7b120508bd71966";
        static string speechRegion = "swedencentral";

        static string customEndpoint = "4fd1dac9-b594-483e-ab6f-e0d3c0c71a75";

        /*
         * Custom pronounciation
         https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-custom-speech-test-and-train#pronunciation-data-for-training
        https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-custom-speech-human-labeled-transcriptions
         */
        static public bool useCustomEndpoint = true;

        /*
         * Using PhraseList as alternative /supplement to improve accuracy on recognizing  
         * Just-in-time: A phrase list is provided just before starting the speech recognition, eliminating the need to train a custom model
         * Csonideration:There are some situations where training a custom model that includes phrases is likely the best option to improve accuracy. For example, in the following cases you would use Custom Speech:
         *  If you need to use a large list of phrases. A phrase list shouldn't have more than 500 phrases.
         *  If you need a phrase list for languages that are not currently supported.
         https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-custom-speech-test-and-train#pronunciation-data-for-training
         */
        static public bool usePhraseList = false;

        static public bool usePredefinedAUdioFile = false;
        async static Task Main(string[] args)
        {
            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            //await FromMic(speechConfig);

            await FromMic(speechConfig);
        }

        async static Task FromMic(SpeechConfig speechConfig)
        {
            Console.WriteLine("Say something...");

            //source: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/get-started-stt-diarization?tabs=windows&pivots=programming-language-csharp
            //source: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-recognize-speech?pivots=programming-language-csharp#change-how-silence-is-handled
            speechConfig.SpeechRecognitionLanguage = "en-US";
            speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "500"); //500 ms default

            /**/
            if (useCustomEndpoint)
            {
                speechConfig.EndpointId = customEndpoint;

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
                // Create a conversation transcriber using audio stream input
                using (var conversationTranscriber = new SpeechRecognizer(speechConfig, audioConfig))
                {

                    if (usePhraseList)
                    {
                        PhraseListGrammar phraseList = PhraseListGrammar.FromRecognizer(conversationTranscriber);
                        phraseList.AddPhrase("CSAM");
                        phraseList.AddPhrase("CAF");
                        phraseList.AddPhrase("WAF");
                        phraseList.AddPhrase("ECIF");
                    }

                    //static void OutputSpeechRecognitionResult(SpeechRecognitionResult speechRecognitionResult)
                    //{
                    //    switch (speechRecognitionResult.Reason)
                    //    {
                    //        case ResultReason.RecognizedSpeech:
                    //            Console.WriteLine($"RECOGNIZED: Text={speechRecognitionResult.Text}");
                    //            break;
                    //        case ResultReason.NoMatch:
                    //            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    //            break;
                    //        case ResultReason.Canceled:
                    //            var cancellation = CancellationDetails.FromResult(speechRecognitionResult);
                    //            Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    //            if (cancellation.Reason == CancellationReason.Error)
                    //            {
                    //                Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    //                Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                    //                Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                    //            }
                    //            break;
                    //    }
                    //}





                    conversationTranscriber.Recognizing += (s, e) =>
                    {
                        Console.WriteLine($"TRANSCRIBING: Text={e.Result.Text}");
                    };

                    conversationTranscriber.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            Console.WriteLine($"TRANSCRIBED: Text={e.Result.Text}");
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

                    await conversationTranscriber.StartContinuousRecognitionAsync();

                    // Waits for completion. Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    await conversationTranscriber.StopContinuousRecognitionAsync();
                }
            }
        }


    }
}