using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;
using System.Linq;
using System;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

public class MusicClassifier : MonoBehaviour
{
    [SerializeField] private AudioClip audioInput;
    [SerializeField] private ModelAsset modelONNX;
    [SerializeField] private BackendType backendType;
    [SerializeField] private int sampleRate = 16000, windowSize = 1024, hopSize = 512, numBins = 128, topNClasses, textureWidth = 1024;
    [SerializeField] private float maxMagnitude = 1f;
    [SerializeField] private MeshRenderer spectrogramRenderer;

    private Texture2D spectrogramTexture;
    private IWorker engine;
    private TensorFloat tensorInput = null;
    private Ops ops;

    private void Start()
    {
        Model model = ModelLoader.Load(modelONNX);
        engine = WorkerFactory.CreateWorker(backendType, model);
        ops = WorkerFactory.CreateOps(backendType, null);

        AudioDescription(audioInput);
    }

    public void AudioDescription(AudioClip audioInput)
    {
        int numSamples = audioInput.samples * audioInput.channels;
        float[] data = new float[numSamples];
        audioInput.GetData(data, 0);
        float[][] spectrogram = ComputeLogMelSpectrogram(data, sampleRate);


        MinMaxNormalizeSpectrogram(spectrogram);
        UpdateSpectrogramTexture(spectrogram);

        NormalizeSpectrogram(spectrogram, 0f, 0.5f);


        float[] flattenedSpectrogram = spectrogram.SelectMany(x => x).ToArray();
        tensorInput = new TensorFloat(new TensorShape(1, 1024, 128), flattenedSpectrogram);


        engine.Execute(tensorInput);

        TensorFloat result = engine.PeekOutput() as TensorFloat;

        TensorFloat probabilities = ops.Softmax(result);
        probabilities.MakeReadable();

        float[] probabilityArray = probabilities.ToReadOnlyArray();
        List<string> classNames = GetClassNames();

        List<(string, float)> classProbabilities = new();
        for(int i = 0; i < probabilityArray.Length; i++)
        {
            classProbabilities.Add((classNames[i], probabilityArray[i]));
        }

        classProbabilities = classProbabilities.OrderByDescending(x => x.Item2).ToList();

        for(int i = 0; i < topNClasses; i++)
        {
            Debug.Log(classProbabilities[i].Item1 + " (" + classProbabilities[i].Item2 + ")");
        }
    }

    private float[][] ComputeSpectrogram(float[] audioData)
    {
        int numFrames = (audioData.Length - windowSize) / hopSize + 1;
        Debug.Log(numFrames);
        float[][] spectrogram = new float[numFrames][];
        for (int i = 0; i < numFrames; i++)
        {
            float[] frame = new float[windowSize];
            Array.Copy(audioData, i * hopSize, frame, 0, windowSize);
            float[] fft = ComputeFFT(frame);
            spectrogram[i] = fft;
        }
        return spectrogram;
    }

    private float[] ComputeFFT(float[] frame)
    {
        Complex[] complexFrame = new Complex[frame.Length];
        for (int i = 0; i < frame.Length; i++)
        {
            complexFrame[i] = new Complex(frame[i], 0);
        }

        Fourier.Forward(complexFrame, FourierOptions.NoScaling);

        float[] magnitude = new float[windowSize/2];
        for (int i = 0; i < windowSize / 2; i++)
        {
            magnitude[i] = (float)complexFrame[i].Magnitude;
        }

        return magnitude;
    }

    private void NormalizeSpectrogram(float[][] spectrogram, float newMean, float newStd)
    {
        // Flatten the spectrogram array
        float[] flattenedSpectrogram = spectrogram.SelectMany(x => x).ToArray();

        // Compute mean and standard deviation
        float mean = flattenedSpectrogram.Average();
        float stdDev = (float)Math.Sqrt(flattenedSpectrogram.Select(x => (x - mean) * (x - mean)).Sum() / flattenedSpectrogram.Length);

        // Normalize spectrogram data
        for (int i = 0; i < spectrogram.Length; i++)
        {
            for (int j = 0; j < spectrogram[i].Length; j++)
            {
                spectrogram[i][j] = (spectrogram[i][j] - mean) / (newStd * stdDev) + newMean;
            }
        }
    }

    private void MinMaxNormalizeSpectrogram(float[][] spectrogram)
    {
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        // Find the minimum and maximum values in the spectrogram
        foreach (float[] row in spectrogram)
        {
            foreach (float value in row)
            {
                if (value < minValue)
                    minValue = value;
                if (value > maxValue)
                    maxValue = value;
            }
        }

        // Apply min-max normalization
        for (int i = 0; i < spectrogram.Length; i++)
        {
            for (int j = 0; j < spectrogram[i].Length; j++)
            {
                spectrogram[i][j] = (spectrogram[i][j] - minValue) / (maxValue - minValue);
            }
        }
    }

    public void UpdateSpectrogramTexture(float[][] spectrogram)
    {
        int textureHeight = spectrogram[0].Length;

        if (spectrogramTexture == null)
        {
            spectrogramTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            spectrogramTexture.filterMode = FilterMode.Point;
            spectrogramTexture.wrapMode = TextureWrapMode.Clamp;
        }

        Color[] colors = new Color[textureWidth * textureHeight];
        int index = 0;
        for (int i = 0; i < textureHeight; i++)
        {
            for (int j = 0; j < textureWidth; j++)
            {
                float magnitude = spectrogram[j][i];
                colors[index++] = new Color(magnitude, 0f, 1 - magnitude);
            }
        }

        spectrogramTexture.SetPixels(colors);
        spectrogramTexture.Apply();

        spectrogramRenderer.material.mainTexture = spectrogramTexture;
    }

    private float[] FlattenedSpectrogram(float[][] spectrogram)
    {
        int cols = spectrogram.Length;
        int rows = spectrogram[0].Length;
        float[] flattenedArray = new float[rows * cols];

        int index = 0;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                flattenedArray[index++] = spectrogram[j][i];
            }
        }

        return flattenedArray;
    }

    private float[][] ComputeLogMelSpectrogram(float[] audioData, int sampleRate)
    {
        // Compute the spectrogram
        float[][] spectrogram = ComputeSpectrogram(audioData);

        // Compute the Mel filterbank
        MelFilterBank melFilterbank = new MelFilterBank(numBins, windowSize / 2, sampleRate, 0f, sampleRate/2);

        // Apply the Mel filterbank to the spectrogram
        float[][] melSpectrogram = melFilterbank.Apply(spectrogram);

        // Take the logarithm of each value
        for (int i = 0; i < melSpectrogram.Length; i++)
        {
            for (int j = 0; j < melSpectrogram[i].Length; j++)
            {
                melSpectrogram[i][j] = Mathf.Log(melSpectrogram[i][j] + 1e-6f); // Add a small value to avoid log(0)
            }
        }

        // Optionally normalize the log-mel spectrogram

        return melSpectrogram;
    }

    private List<string> GetClassNames()
    {
        List<string> classNames = new();
        for(int i = 0; i < AudioClassifierDictionary.dictionary.Count; i++) classNames.Add(AudioClassifierDictionary.dictionary[i]);
        return classNames;
    }

    private void OnDestroy()
    {
        engine.Dispose();
        tensorInput.Dispose();
    }
}
