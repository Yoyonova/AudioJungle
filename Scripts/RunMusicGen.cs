// CODE FROM UNITY SENTIS

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;
using Newtonsoft.Json;


//      Inference for MusicGen-300
//      ==========================
//
//  Details
//  -------
//  The model predicts 4 streams of codes staggered like this:
//  * * * a b c
//  * * a b c
//  * a b c 
//  a b c
//  Then aligns the streams so that it groups all the a's togther etc.

// Put sentis files and json file in Assets/StreamingAssets folder
// Put this script on the Main Camera object
// Put an audiosource on the Main Camera
// Press play and see console window for updates

// See https://github.com/huggingface/transformers/blob/main/src/transformers/models/musicgen/modeling_musicgen.py


public class RunMusicGen : MonoBehaviour
{
    [SerializeField] RectTransform loadingBar;
    [SerializeField] float loadingBarWidth = 280;

    //Change this prompt to whatever you like:
    public string prompt = "80s pop track with bassy drums and synth";
    // number of seconds to create clip for (up to 30 seconds)
    [SerializeField] private int seconds = 2;
    // Make this value smaller to make music more random
    [SerializeField] private float predictability = 1f;
    public static AudioClip clip;


    BackendType backendType = BackendType.GPUCompute;

    IWorker toWavEngine, decoderEngine, textEngine, projectEngine;

    const int numCodeBooks = 4;

    // Special music decoder tokens
    const int DECODER_START_TOKEN = 2048;

    // Special text encoder tokens
    const int END_TEXT_TOKEN = 1;

    int decoderTokens; //text tokens

    List<int> tokensSoFar = new();
    TensorFloat encoder_hidden_states;
    TensorInt encoder_attention_mask, input_ids;
    Ops ops;
    Model decoder;

    // How much to stagger each code stream by wrt the next one
    int DELAY = 1;

    // Vocab list
    List<string> tokens = new List<string>(); 

    //The output frequency must be 32kHz
    const int outputFrequency = 32000;

    int maxFrames;

    List<int> TOKENS;

    int frame = 0;
    bool hasDecodedMusic = false;
    void Start()
    {
        ops = WorkerFactory.CreateOps(backendType, null);

        maxFrames = 50 * seconds + 3;
        
        LoadVocab(); 

        TOKENS = GetTokens(prompt);

        Debug.Log("Parsed tokens=\n" + string.Join(",", TOKENS));
        
        CreateAttentionMask();
        ParseText();
        LoadDecoderModel();
        
        SetupMusicCodeStreams();
        
        frame = 0;
    }

    void LoadDecoderModel()
    {
        decoder = ModelLoader.Load(Application.streamingAssetsPath + "/decoder.sentis");
        decoderEngine = WorkerFactory.CreateWorker(backendType, decoder);
    }

    void CreateAttentionMask()
    {
        int[] mask = new int[1 * decoderTokens];
        for (int i = 0; i < mask.Length; i++) mask[i] = 1;
        encoder_attention_mask = new TensorInt(new TensorShape(1, decoderTokens), mask);
    }

    void SetupMusicCodeStreams()
    {
        //Sets the staggered start codes 
        tokensSoFar.AddRange(new int[numCodeBooks * maxFrames]);
        for (int j = 0; j < maxFrames; j++)
        {
            for (int i = 0; i < numCodeBooks; i++)
            {
                if ( i * DELAY >= j)
                {
                    tokensSoFar[i * maxFrames + j] = DECODER_START_TOKEN;
                }
                else
                {
                    tokensSoFar[i * maxFrames + j] = -1;
                }
            }
        }
        input_ids = new TensorInt(new TensorShape(numCodeBooks, maxFrames), tokensSoFar.ToArray());
    }

    List<int> GetTokens(string text)
    {
        //split over whitespace
        string[] words = text.ToLower().Split(null);
        for (int i = 0; i < words.Length; i++) words[i] = " " + words[i];

        var ids = new List<int>();

        string s = "";

        foreach (var word in words)
        {
            int start = 0;
            for (int i = word.Length; i >= 0; i--)
            {
                string subword = word.Substring(start, i - start);
                int index = tokens.IndexOf(subword);
                if (index >= 0)
                {
                    ids.Add(index);
                    s += subword + " ";
                    if (i == word.Length) break;
                    start = i;
                    i = word.Length + 1;
                }
            }
        }

        ids.Add(END_TEXT_TOKEN);

        decoderTokens = ids.Count;

        Debug.Log("Tokenized sentece = " + s);

        return ids;
    }

    void ParseText()
    {
        Model textencoder = ModelLoader.Load(Application.streamingAssetsPath + "/textencoder.sentis");
        textEngine = WorkerFactory.CreateWorker(BackendType.GPUCompute, textencoder);

        Model project = ModelLoader.Load(Application.streamingAssetsPath + "/project768_1024.sentis");
        projectEngine = WorkerFactory.CreateWorker(BackendType.GPUCompute, project);
        
        using var input = new TensorInt(new TensorShape(1, decoderTokens), TOKENS.ToArray());

        var inputs = new Dictionary<string, Tensor>
        {
            {"input_ids", input },
            {"attention_mask", encoder_attention_mask }
        };
        textEngine.Execute(inputs);

        var output = textEngine.PeekOutput() as TensorFloat;

        //Convert vectors of size 768 to size 1024
        projectEngine.Execute(output);
        encoder_hidden_states = projectEngine.PeekOutput() as TensorFloat;
        encoder_hidden_states.TakeOwnership();
    }

    private class TokenizerData
    {
        public ModelData model;
    }
    private class ModelData
    {
        public object[][] vocab;
    }

    void LoadVocab()
    {
        var data = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenizerData>(System.IO.File.ReadAllText(
            Application.streamingAssetsPath+"/tokenizer.json"
            ));
        for(int i = 0; i < data.model.vocab.Length; i++)
        {
            string tokenName = (string)data.model.vocab[i][0];
            tokens.Add(tokenName);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (frame < maxFrames)
        {
            GetOneMusicToken();
        }
        else if(!hasDecodedMusic)
        {
            hasDecodedMusic = true;
            DecodeMusic();
        }
        frame++;
    }

    void GetOneMusicToken()
    {
        var inputs = new Dictionary<string, Tensor>
        {
            {"input_ids", input_ids },
            {"encoder_hidden_states", encoder_hidden_states },
            {"encoder_attention_mask" , encoder_attention_mask }
        };

        decoderEngine.Execute(inputs);
        var decoderOutput = decoderEngine.PeekOutput() as TensorFloat;                                                                     
        using var dec2 = ops.Mul(decoderOutput, predictability);
        using var probs = ops.Softmax(dec2, 2);
        probs.MakeReadable();

        int OFFSET = 1;

        //Add new tokens to code streams
        for (int j = 0; j < numCodeBooks; j++)
        {
            if (frame < maxFrames - OFFSET)
            {
                int N = j * maxFrames + frame + OFFSET;

                if (tokensSoFar[N] != DECODER_START_TOKEN)
                {
                    tokensSoFar[N] = SelectRandomToken(probs, j, frame);
                }
            }
        }
        Replace(ref input_ids, new TensorInt(input_ids.shape, tokensSoFar.ToArray()));
        Debug.Log("Frame=" + frame + "/" + maxFrames);

        loadingBar.sizeDelta = new Vector2(frame * loadingBarWidth / maxFrames, loadingBar.sizeDelta.y);
    }

    int SelectRandomToken(TensorFloat probs,int j, int frame)
    {
        int numItems = probs.shape[2];
        float p = UnityEngine.Random.Range(0, 1f);
        float tot = 0;
        for(int i = 0; i < numItems; i++)
        {
            tot += probs[j, frame, i];
            if (p <= tot) return i;
        }
        return numItems - 1;
    }
    void LoadMusicTokensToWavModel()
    {
        if (toWavEngine != null) return;
        Model toWav = ModelLoader.Load(Application.streamingAssetsPath + "/encodec.sentis");
        toWavEngine = WorkerFactory.CreateWorker(BackendType.GPUCompute, toWav);
    }

    void DecodeMusic()
    {
        Debug.Log("Please wait while music is decoded...");
        LoadMusicTokensToWavModel();

        using var input2 = AlignCodeStreams(input_ids);
        using var wavTokens = input2.ShallowReshape(new TensorShape(1, 1, numCodeBooks, maxFrames - 3));
        
        toWavEngine.Execute(wavTokens);
        var output = toWavEngine.PeekOutput() as TensorFloat;
        output.MakeReadable();

        int numSamples = Mathf.Min(output.shape.length, outputFrequency * seconds);
        Debug.Log("Number of samples=" + numSamples + " / " + output.shape.length);
        clip = AudioClip.Create("music", numSamples, 1, outputFrequency, false);

        float[] wav = new float[numSamples];
        System.Array.Copy(output.ToReadOnlyArray(), wav, numSamples);
        clip.SetData(wav, 0);
    }

    TensorInt AlignCodeStreams(TensorInt input)
    {
        if (DELAY == 0)
        {
            return ops.Copy(input);
        }
        using var input2 = ops.Cast(input, DataType.Float);
        TensorFloat[] B = new TensorFloat[4];
        for (int i = 0; i < 4; i++) {
            using TensorFloat A = ops.Slice(input2, new int[] { i }, new int[] { i + 1 }, new int[] { 0 }, new int[] { 1 }) as TensorFloat;
            B[i] = ops.Pad(A, new int[] { 0, -i, 0, i - 3 });
        }
        using var input3 = ops.Concat(B, 0) as TensorFloat;
        for(int i = 0; i < 4; i++)
        {
            B[i].Dispose();
        }
        return ops.Cast(input3, DataType.Int) as TensorInt;
    }

    void Replace<T>(ref T A, T B) where T:System.IDisposable
    {
        A?.Dispose();
        A = B;
    }

    private void OnDestroy()
    {
        input_ids?.Dispose();
        encoder_attention_mask?.Dispose();
        encoder_hidden_states?.Dispose();
        ops?.Dispose();
        decoderEngine?.Dispose();
        toWavEngine?.Dispose();
        projectEngine?.Dispose();
        textEngine?.Dispose();
    }
}

