using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace DenisVizigin.Sound
{
    public class SoundParser
	{
		int max = 100;
		float start = 0; 
		public Slider loadingSlider;
		public int HistoryBuffer = 44;

		private double[] _FFTBytes;
		private double[] _lastFFTBytes;
		
		private int[] _detectedBeats;
		private double[] _spectralFlux;
		private double[] _rawBytes;
		private double[,] _beats;

		private List<List<double>> _energyHistoryBuffer;
		
		private AudioClip _sound;
		private int _totalSamples;
		private int _totalBeats = 0;
		private int _parseSampleCount = 0;

		private double _beatSensitivity;
		private int _beatSubbands;
		private int _sampleSize;
		private int _soundFeed;


		void Start(){
			loadingSlider = GameObject.Find("LoadingSlider").GetComponent<Slider>();
			loadingSlider.maxValue = max;
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="DenisVizigin.Sound.SoundParser"/> class.
		/// </summary>
		/// <param name="sound">Main audioClip object for analyzing soundwave</param>
		/// <param name="sampleSize">Size of block in samples (can be 1024, 2048, 4096 etc.)</param>
		/// <param name="soundFeed">How many blocks analyzed in one Update's method call</param>
		/// <param name="beatSensitivity">Beat sensitivity</param>
		public SoundParser(AudioClip sound, int sampleSize, int soundFeed, int beatSubbands, double beatSensitivity)
		{
			_sound = sound;
			_soundFeed = soundFeed;
			_sampleSize = sampleSize;
			_beatSubbands = beatSubbands;
			_beatSensitivity = beatSensitivity;

			_totalSamples = (int)(sound.samples / _sampleSize);
			_rawBytes = new double[_sampleSize];
			_FFTBytes = new double[_sampleSize / 2];
			_lastFFTBytes = new double[_sampleSize / 2];
			
			_spectralFlux = new double[TotalSamples];

			_beats = new double[TotalSamples, beatSubbands];
			_detectedBeats = new int[TotalSamples];
			_energyHistoryBuffer = new List<List<double>>();
		}

		/// <summary>
		/// Just section's parse mechanics with calling sound analyzing
		/// method WriteData in loop
		/// </summary>
		public bool Parse()
		{
			if (_parseSampleCount >= TotalSamples)
				return true;
			
			for (int j = 0; j < _soundFeed; j++)
			{
				if (_parseSampleCount + j >= TotalSamples) 
					break;
				WriteData(_parseSampleCount + j);
			}

			_parseSampleCount = (_parseSampleCount + _soundFeed > TotalSamples) ? TotalSamples : _parseSampleCount + _soundFeed;
			return false;
		}

		/// <summary>
		/// Analyzing sound function make in 5 stages:
		/// 1. Get sound data by GetData method;
		/// 2. Run Hamming window for smoothing;
		/// 3. Run FTT for calculate magnitutes;
		/// 4. Calculate beats from this algoritm: http://archive.gamedev.net/archive/reference/programming/features/beatdetection/;
		/// 5. Calculate spectral flux; 
		/// </summary>
		/// <param name="currentSample">Current sample.</param>
		private void WriteData(int currentSample)
		{
			int startPosition = currentSample * _sampleSize;
            float[] samples = new float[_sampleSize * _sound.channels]; 
            _sound.GetData(samples, startPosition);

			if (_sound.channels == 1)
			{
				for (int i = 0; i < samples.Length; i++)
		        	_rawBytes[i] = samples[i];
		    }
		    else
		    {
		    	for (int i = 0; i < samples.Length; i = i + 2) {
		        	_rawBytes[i / 2] = .5 * (samples[i] + samples[i+1]);
		    	}
		    }

			RunHammingWindow();
			RunFFT(10, false);
			_spectralFlux[currentSample] = (currentSample < 1) ? 0 : CalculateSpectralFlux();
			CalculateBeats(currentSample);

			// Just copy built-in arrays
			for (int i = 0; i < _FFTBytes.Length; i++)
		       	_lastFFTBytes[i] = _FFTBytes[i];
		}

		/// <summary>
		/// Method smoothing raw bytes by hamming function
		/// More: http://en.wikipedia.org/wiki/Window_function#Generalized_Hamming_windows
		/// </summary>
        private void RunHammingWindow()
		{
			for (int i = 0; i < _rawBytes.Length; i++)
				_rawBytes[i] = (0.54 + 0.46 * Math.Cos(2 * Math.PI * i / _rawBytes.Length)) * _rawBytes[i];
		}

		/// <summary>
		/// Run FFT function and then compute magnitutes of real and imagnary parts of sound.
		/// </summary>
		/// <param name="logN">Log2 of FFT length. e.g. for 512 pt FFT, logN = 9</param>
		/// <param name="inverse">If set to <c>true</c> inverse.</param>
		private void RunFFT(uint logN, bool inverse = false)
		{
			double[] re = new double[_rawBytes.Length];
			double[] im = new double[_rawBytes.Length];

			for (int j = 0; j < _rawBytes.Length; j++)
			{
				re[j] = _rawBytes[j];
				im[j] = 0;
			}
			
			FFT fft = new FFT();
			fft.init(logN);
			fft.run(re, im, inverse);

			for (int i = 0; i < _FFTBytes.Length; i++)
				_FFTBytes[i] = Math.Sqrt(re[i] * re[i] + im[i] * im[i]);
		}

		/// <summary>
		/// Calculates the spectral flux
		/// </summary>
		/// <returns>The spectral flux</returns>
       	private double CalculateSpectralFlux()
		{
			double diff = 0;
			double flux = 0;
			
			for (int i = 0; i < _FFTBytes.Length; i++)
			{	
				diff = _FFTBytes[i] - _lastFFTBytes[i];
				flux += ((diff < 0) ? 0 : diff);
			}

			return flux;
		}

		/// <summary>
		/// Beat Detector
		/// Calculates the beats by this algoritm: http://archive.gamedev.net/archive/reference/programming/features/beatdetection/;
		/// It algoritm used in all libs but it's not so good. Better use onset detection
		/// </summary>
		/// <param name="fftBytes">fft bytes</param>
		/// <param name="currentSample">Current analyzing sample</param>
		private void CalculateBeats(int currentSample) 
		{
			double[] es = new double[_beatSubbands];
			int detectedBeats = 0;
			int i = 0;

			// init beats on the line (zero = no beats)
			for (i = 0; i < _beatSubbands; i++) {
				_detectedBeats [currentSample] = 0;
				_beats [currentSample, i] = es [i] = 0.0;
			}

			// divide all bytes into _beatSubbands parts
			for (i = 0; i < _FFTBytes.Length; i++)
			{
				int c = (int)((float)i / ((float)_FFTBytes.Length / (float)_beatSubbands));
				es[c] += _FFTBytes[i];
			}

			if (currentSample % HistoryBuffer == 0)
			{
				if (_energyHistoryBuffer.Count == HistoryBuffer)
				{
					for (i = 0; i < es.Length; i++)
					{
						if (es[i] > _beatSensitivity * ComputeAverageHistoryEnergy(i))
						{
							_beats[currentSample,i] = es[i];
							_totalBeats++;
							detectedBeats++;
						}
					}

					_detectedBeats[currentSample] = detectedBeats;
				}
			}
			
			AddHistoryEnergy(es);
		}

		/// <summary>
		/// Computes the average history energy
		/// </summary>
		/// <returns>The average history energy</returns>
		/// <param name="subband">Subband</param>
		private double ComputeAverageHistoryEnergy(int subband)
		{
			double averageEnergy = 0;
			for (int i = 0; i < _energyHistoryBuffer.Count; i++)
				averageEnergy += _energyHistoryBuffer[i][subband];
			return averageEnergy / (double)_energyHistoryBuffer.Count;
		}

		/// <summary>
		/// Method control energy history buffer size (HistoryBuffer) by queue algoritm and add new energy value into list
		/// </summary>
		/// <param name="es">Energy value</param>
		private void AddHistoryEnergy(double[] es)
		{
			if (_energyHistoryBuffer.Count >= HistoryBuffer)
				_energyHistoryBuffer.RemoveAt(0);

			for (int i = 0; i < es.Length; i++) {
				if (i == 0) _energyHistoryBuffer.Add( new List<double>(_beatSubbands) );
				_energyHistoryBuffer[_energyHistoryBuffer.Count - 1].Add(es[i]);
			}
		}
		
		public int ParseSampleCount { get { return _parseSampleCount; } }
		
		public int TotalSamples { get { return _totalSamples; } }
		
		public double[] SpectralFlux { get { return _spectralFlux; } }

		public double[,] Beats { get { return _beats; } }
		
		public int[] DetectedBeats { get { return _detectedBeats; } }

		public int TotalBeats { get { return _totalBeats; } }
    }
}