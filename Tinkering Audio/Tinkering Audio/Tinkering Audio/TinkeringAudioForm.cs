﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio;
using NAudio.Wave;

namespace TinkeringAudio {
    public partial class TinkeringAudioForm : Form {
        private readonly int SAMPLE_RATE = 44100;
        private readonly int MAX_VALUE = (int)Math.Pow(2, 15);

        private double volume = 0.08;

        private delegate double WaveFunction(double frequency, int position);
        private WaveFunction waveFunction = null;

        private WaveOut waveOut = null;
        private IWaveProvider waveProvider = null;

        private List<double> notes;
        private double[] noteDuration;

        public TinkeringAudioForm() {
            InitializeComponent();
        }

        private void TinkeringAudioForm_Load(object sender, EventArgs e) {
            this.waveFunction = SawtoothWave;
            notes = PopulateNotes(440, -16, 8, 2);
            noteDuration = new double[] { 0.15, 0.2, 0.3, 0.4 };
        }

        private List<int> GenerateSilence(double durationInSeconds) {
            List<int> silence = new List<int>();

            for (int i = 0; i < (int)(durationInSeconds * SAMPLE_RATE); i++) {
                silence.Add(0);
            }
            return silence;
        }

        private List<int> GenerateTone(double durationInSeconds, WaveFunction waveFunction, double[] frequencies) {
            List<int> tone = new List<int>();

            short value;

            for (int i = 0; i < (int)(durationInSeconds * SAMPLE_RATE); i++) {
                value = 0;
                for (int j = 0; j < frequencies.Length; j++) {
                    value += (short)(MAX_VALUE * volume * waveFunction.Invoke(frequencies[j], i));
                }
                tone.Add(BitConverter.GetBytes(value)[0]);
            }
            return tone;
        }

        private List<int> GenerateRandomMelody(int countOfNotesToPlay) {
            Random prng = new Random();

            List<int> melody = new List<int>();
            melody.AddRange(GenerateSilence(0.1));

            double[] frequencies = new double[2];

            for (int i = 0; i < countOfNotesToPlay; i++) {
                frequencies[0] = GetRandomElement(notes, prng);
                //frequencies[1] = GetRandomElement(notes, prng);

                melody.AddRange(GenerateTone(GetRandomElement(noteDuration, prng), this.waveFunction, frequencies));
            }
            return melody;
        }

        private List<int> GenerateWhiteNoise(int durationInSeconds) {
            Random prng = new Random();

            List<int> noise = new List<int>();

            for (int i = 0; i < (int)(durationInSeconds * SAMPLE_RATE); i++) {
                int value = (int)(prng.Next(-1, 1) * volume * MAX_VALUE);
                noise.Add(value);
            }

            return noise;
        }

        private static T GetRandomElement<T>(IEnumerable<T> enumerable, Random rng) {
            int index = rng.Next(0, enumerable.Count());
            return enumerable.ElementAt(index);
        }

        private List<double> PopulateNotes(double baseNote, int startNote, int endNote, int increment) {
            double ESTIMATOR = Math.Pow(2.0, (1.0 / 12.0));

            List<double> notes = new List<double>();

            for (int i = startNote; i < endNote; i += increment) {
                notes.Add(baseNote * Math.Pow(ESTIMATOR, i));
            }
            return notes;
        }

        private IWaveProvider convertToWaveProvider16(List<int> sample, int sampleRate, int channelCount) {
            byte[] byteBuffer = new byte[sample.Count * 2];

            int byteArrayIndex = 0;
            short value;

            for (int i = 0; i < sample.Count; i++) {
                if (sample[i] > MAX_VALUE) {
                    value = (short)MAX_VALUE;
                } else if (sample[i] < -MAX_VALUE) {
                    value = (short)-MAX_VALUE;
                } else {
                    value = (short)sample[i];
                }

                byteBuffer[byteArrayIndex++] = BitConverter.GetBytes(value)[0];
                byteBuffer[byteArrayIndex++] = BitConverter.GetBytes(value)[1];
            }

            IWaveProvider waveProvider = new RawSourceWaveStream(new MemoryStream(byteBuffer), new WaveFormat(sampleRate, 16, channelCount));

            return waveProvider;
        }

        private double SquareWave(double frequency, int position) {
            double value = Math.Sin(2.0 * Math.PI * frequency * (position / (double)SAMPLE_RATE));

            if (value > 0) {
                return 1.0;
            } else {
                return -1.0;
            }
        }

        private double SinWave(double frequency, int position) {
            return Math.Sin(2.0 * Math.PI * frequency * (position / (double)SAMPLE_RATE));
        }

        private double TriangleWave(double frequency, int position) {
            double value = ((2.0 * MAX_VALUE * volume) / Math.PI) *Math.Asin(Math.Sin(2.0 * Math.PI * frequency * position));
            return value;
        }

        private double SawtoothWave(double frequency, int position) {
            double value = (-(2.0 * MAX_VALUE * volume) / Math.PI) * Math.Atan((1 / Math.Tan(Math.PI * frequency * position)));
            return value;
        }

        private void btn_GenerateMelody_Click(object sender, EventArgs e) {
            waveProvider = convertToWaveProvider16(GenerateRandomMelody(12), SAMPLE_RATE, 1);

            waveOut = new WaveOut();
            waveOut.Init(waveProvider);
            waveOut.Play();
        }

        private void btn_SaveMelody_Click(object sender, EventArgs e) {
            waveProvider = convertToWaveProvider16(GenerateRandomMelody(12), SAMPLE_RATE, 1);

            string filename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");

            WaveFileWriter.CreateWaveFile(filename, waveProvider);
        }

        private void btn_GenerateWhiteNoise_Click(object sender, EventArgs e) {
            WaveOut waveOut = new WaveOut();
            waveOut.Init(convertToWaveProvider16(GenerateWhiteNoise(12), SAMPLE_RATE, 1));
            waveOut.Play();
        }
    }
}