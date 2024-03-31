using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;
using System.Linq;
using System;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

public class AudioTranscriber : MonoBehaviour
{
    [SerializeField] private AudioClip audioInput;
    [SerializeField] private ModelAsset modelONNX;
    [SerializeField] private BackendType backendType;
    [SerializeField] private int windowSize = 1024, hopSize = 512, numBins = 128, topNClasses, textureWidth = 1024, textureHeight = 128;
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

        AudioTranscription(audioInput);
    }

    public void AudioTranscription(AudioClip audioInput)
    {
        int numSamples = audioInput.samples * audioInput.channels;
        float[] data = new float[numSamples];
        audioInput.GetData(data, 0);
        float[][] spectrogram = ComputeSpectrogram(data);


        NormalizeSpectrogram(spectrogram);
        UpdateSpectrogramTexture(spectrogram);


        float[] flattenedSpectrogram = spectrogram.SelectMany(x => x).ToArray();
        tensorInput = new TensorFloat(new TensorShape(1, 1024, 128), flattenedSpectrogram);


        engine.Execute(tensorInput);

        TensorFloat result = engine.PeekOutput() as TensorFloat;
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

        float[] magnitude = new float[numBins];
        for (int i = 0; i < numBins; i++)
        {
            magnitude[i] = (float)complexFrame[i].Magnitude;
        }

        return magnitude;
    }

    private void NormalizeSpectrogram(float[][] spectrogram)
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
                spectrogram[i][j] = (spectrogram[i][j] - mean) / (0.5f * stdDev);
            }
        }
    }

    public void UpdateSpectrogramTexture(float[][] spectrogram)
    {
        if (spectrogramTexture == null)
        {
            spectrogramTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            spectrogramTexture.filterMode = FilterMode.Point;
            spectrogramTexture.wrapMode = TextureWrapMode.Clamp;
        }

        Color[] colors = new Color[textureWidth * textureHeight];
        for (int i = 0; i < textureWidth; i++)
        {
            for (int j = 0; j < textureHeight; j++)
            {
                float magnitude = spectrogram[i][j] / 2 + 0.5f;
                colors[j * textureWidth + i] = new Color(magnitude, magnitude, magnitude);
            }
        }

        spectrogramTexture.SetPixels(colors);
        spectrogramTexture.Apply();

        spectrogramRenderer.material.mainTexture = spectrogramTexture;
    }

    private void OnDestroy()
    {
        engine.Dispose();
        tensorInput.Dispose();
    }
}
