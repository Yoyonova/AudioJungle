using System;
using UnityEngine;

public class MelFilterBank
{
    private float[][] filterbank;
    private int numFilters;
    private int fftSize;
    private float sampleRate;
    private float minFreq;
    private float maxFreq;

    public MelFilterBank(int numFilters, int fftSize, float sampleRate, float minFreq, float maxFreq)
    {
        this.numFilters = numFilters;
        this.fftSize = fftSize;
        this.sampleRate = sampleRate;
        this.minFreq = minFreq;
        this.maxFreq = maxFreq;

        ComputeFilterbank();
    }

    public float[][] Apply(float[][] spectrogram)
    {
        float[][] melSpectrogram = new float[spectrogram.Length][];

        for (int i = 0; i < spectrogram.Length; i++)
        {
            melSpectrogram[i] = new float[numFilters];
            for (int j = 0; j < numFilters; j++)
            {
                float sum = 0.0f;
                for (int k = 0; k < spectrogram[0].Length; k++)
                {
                    sum += spectrogram[i][k] * filterbank[k][j];
                }
                melSpectrogram[i][j] = sum;
            }
        }
        return melSpectrogram;
    }

    private void ComputeFilterbank()
    {
        filterbank = new float[fftSize][];
        float[] melFrequencies = ComputeMelFrequencies();
        float[] fftFrequencies = ComputeFFTFrequencies();

        for (int i = 0; i < fftSize; i++)
        {
            filterbank[i] = new float[numFilters];

            for (int j = 0; j < numFilters; j++)
            {
                filterbank[i][j] = TriangularFilter(fftFrequencies[i], melFrequencies[j], melFrequencies[j + 1]);
            }
        }
    }

    private float TriangularFilter(float x, float left, float center)
    {
        float right = center + center - left;
        if (x < left || x > right) return 0;
        return 1 - Mathf.Abs((x - center) / (center - left));
    }

    private float[] ComputeMelFrequencies()
    {
        float[] melFrequencies = new float[numFilters + 2];
        float melMin = HzToMel(minFreq);
        float melMax = HzToMel(maxFreq);

        for (int i = 0; i < numFilters + 2; i++)
        {
            melFrequencies[i] = MelToHz(melMin + i * ((melMax - melMin) / (numFilters + 1)));
        }
        return melFrequencies;
    }

    private float[] ComputeFFTFrequencies()
    {
        float[] fftFrequencies = new float[fftSize];
        float melFreqDelta = (maxFreq - minFreq) / (fftSize - 1);

        for (int i = 0; i < fftSize; i++)
        {
            fftFrequencies[i] = minFreq + melFreqDelta * i;
        }
        return fftFrequencies;
    }

    private float HzToMel(float hz)
    {
        return 2595 * (float)Math.Log10(1 + hz / 700);
    }

    private float MelToHz(float mel)
    {
        return 700 * ((float)Math.Pow(10, mel / 2595) - 1);
    }

    private float[][] ConvertToPowerSpectrogram(float[][] spectrogram)
    {
        float[][] powerSpectrogram = new float[spectrogram.Length][];
        for (int i = 0; i < spectrogram.Length; i++)
        {
            powerSpectrogram[i] = new float[spectrogram[0].Length];
            for (int j = 0; j < spectrogram[0].Length; j++)
            {
                powerSpectrogram[i][j] = spectrogram[i][j] * spectrogram[i][j];
            }
        }
        return powerSpectrogram;
    }
}
