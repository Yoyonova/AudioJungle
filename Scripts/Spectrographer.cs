using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

public class Spectrographer : MonoBehaviour
{
    [SerializeField] private int windowSize = 1024, hopSize = 512, numBins = 128;
    [SerializeField] private MeshRenderer spectrogramRenderer;

    public Texture2D spectrogramTexture;

    public static float[][] spectrogram;
    public static Spectrographer instance;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (MusicController.instance.isMusicPlaying && spectrogram == null) CreateSpectrogram(MusicController.instance.audioSource.clip);

        if (SkyboxGenerator.isGenerated && spectrogramTexture == null) UpdateSpectrogramTexture(spectrogram);
    }

    public static float[] GetSpectrumData(int length)
    {
        if (!MusicController.instance.isMusicPlaying) return null;
        float[] data = new float[length];
        MusicController.instance.audioSource.GetSpectrumData(data, 0, FFTWindow.Rectangular);
        return data;
    }

    public static float[] CurrentSpectrogramSlice()
    {
        if (spectrogram == null) return null;

        //int index = 2 * (MusicController.ClipTimeSamples() + instance.windowSize/2) / instance.hopSize;
        int index = (int) (spectrogram.Length * (MusicController.ClipTimeSamples() / (float) MusicController.instance.audioSource.clip.samples));
        index = Mathf.Clamp(index, 0, spectrogram.Length-1);

        Debug.Log("Spectrogram: " + MusicController.ClipTimeSamples() + "/" + MusicController.instance.audioSource.clip.samples);

        return spectrogram[index];
    }

    public static float currentVolume()
    {
        float[] currentAudio = CurrentSpectrogramSlice();
        if (currentAudio == null || currentAudio.Length == 0) return 1;

        float sum = 0;
        foreach (float f in currentAudio) sum += f;
        return sum / currentAudio.Length;
    }

    public void CreateSpectrogram(AudioClip audioInput)
    {
        int numSamples = audioInput.samples * audioInput.channels;
        float[] data = new float[numSamples];
        audioInput.GetData(data, 0);
        spectrogram = ComputeLogMelSpectrogram(data, MusicController.instance.sampleRate);
        spectrogram = CutOffEdges(spectrogram, 1);

        NormalizeSpectrogram(spectrogram, 1f, 0.25f);
    }

    private float[][] CutOffEdges(float[][] spectrogram, int border)
    {
        float[][] cutSpectrogram = new float[spectrogram.Length][];

        for(int i = 0; i < spectrogram.Length; i++)
        {
            cutSpectrogram[i] = new float[spectrogram[i].Length - 2 * border];
            for (int j = 0; j < cutSpectrogram[i].Length; j++) cutSpectrogram[i][j] = spectrogram[i][j + border];
        }

        return cutSpectrogram;
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

    private float[] ComputeFFT(float[] frame) // WRITTEN USING CHAT GPT
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

    private void NormalizeSpectrogram(float[][] spectrogram, float newMean, float newStd) // WRITTEN USING CHAT GPT
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

    private void MinMaxNormalizeSpectrogram(float[][] spectrogram) // WRITTEN USING CHAT GPT
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
        int textureWidth = spectrogram.Length;
        int textureHeight = spectrogram[0].Length;

        if (spectrogramTexture == null)
        {
            spectrogramTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            spectrogramTexture.filterMode = FilterMode.Point;
            spectrogramTexture.wrapMode = TextureWrapMode.Clamp;
        }

        Color[] colors = new Color[textureWidth * textureHeight];
        Color color1 = SkyboxGenerator.GenerateRandomRelatedColor();
        Color color2 = SkyboxGenerator.GenerateRandomRelatedColor();
        int index = 0;
        for (int i = 0; i < textureHeight; i++)
        {
            for (int j = 0; j < textureWidth; j++)
            {
                float magnitude = spectrogram[j][i] - 0.5f;
                colors[index++] = Color.Lerp(color1, color2, magnitude);
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

    private float[][] ComputeLogMelSpectrogram(float[] audioData, int sampleRate) // WRITTEN USING CHAT GPT
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
}
