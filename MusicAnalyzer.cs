using System;
using UnityEngine;
using UnityEngine.UI;

namespace DenisVizigin.Sound
{
    public class MusicAnalyzer
    {
		private float _thresholdMultiplier;

        private double[] _threshold;

        private double[] _peaks;
		
        private SoundParser _soundParser;
		
		private double _sumOfFluxThresholds;

		private int _thresholdSize;

		int max = 100;
		float start = 0; 
		GameObject loadingSlider = Main.slider;
		/// <summary>
		/// Initializes a new instance of the <see cref="DenisVizigin.Sound.MusicAnalyzer"/> class.
		/// MusicAnalyzer creates array of 3d track heights (0..1) analizing sound parameters. 
		/// </summary>
		/// <param name="sound">Main audioClip object for analyzing soundwave</param>
		/// <param name="sampleSize">Size of block in samples (can be 1024, 2048, 4096 etc.)</param>
		/// <param name="soundFeed">How many blocks analyzed in one Update's method call</param>
		/// <param name="beatSensitivity">Beat sensitivity of Beat Detector</param>
		/// <param name="thresholdSize">Threshold size</param>
		/// <param name="thresholdMultiplier">Threshold multiplier</param>

		void Start(){
			loadingSlider = GameObject.Find("LoadingSlider");
			loadingSlider.GetComponent<Slider>().maxValue = max;
		}

		public MusicAnalyzer(AudioClip sound, 
		                     int sampleSize = 1024, 
		                     int soundFeed = 40,
		                     int beatSubbands = 3, 
		                     double beatSensitivity = 1.5, 
		                     int thresholdSize = -1, 
		                     float thresholdMultiplier = 1.5f)
		{
			_thresholdMultiplier = thresholdMultiplier;
			// THRESHOLD WINDOW SIZE (if -1 then autoSize)
			_thresholdSize = (thresholdSize < 0) ? (int)(3 * sound.length) : thresholdSize;

			_soundParser = new SoundParser( sound, sampleSize, soundFeed, beatSubbands, beatSensitivity );
			_threshold = new double[_soundParser.TotalSamples];
			_peaks = new double[_soundParser.TotalSamples];
		}

		/// <summary>
		/// Analyze method calls when MonoBehaviour calls Update method.
		/// It call main Parse method of the _soundParser object. After parse
		/// MusicAnalyzer calculate FluxThresholds, smooth it by Kalman's filter
		/// and convert in 1..0 representation.
		/// </summary>
		public bool Analyze()
		{
			if (_soundParser.Parse())
			{
				CalculateFluxThresholds();
				DetectPeaks();
				CalculateKalmanFilter();
				ConvertPercents();
				CalculateSumOfThresholds();
				return true;
			}
			else
			{
				loadingSlider.GetComponent<Slider>().value = (float)(100 * Math.Round((float)_soundParser.ParseSampleCount / (float)_soundParser.TotalSamples, 2));
				Debug.Log( "Parsed " + 100 * Math.Round((float)_soundParser.ParseSampleCount / (float)_soundParser.TotalSamples, 2) + "% of sound" );
				return false;
			}
		}
		
		/// <summary>
		/// Calculates the flux thresholds.
		/// </summary>
		private void CalculateFluxThresholds()
		{
			for( int i = 0; i < _soundParser.TotalSamples; i++ )
			{
				int start = Math.Max( 0, i - _thresholdSize / 2 );
				int end = Math.Min( _soundParser.SpectralFlux.Length - 1, i + _thresholdSize / 2 );
				double mean = 0;
				for(int j = start; j <= end; j++ )
					mean += _soundParser.SpectralFlux[j];
				mean /= (end - start);
				_threshold[i] = mean * _thresholdMultiplier;
			}
		}

		/// <summary>
		/// Alternate way to detect peaks.
		/// </summary>
		private void DetectPeaks()
		{
			double[] prunnedSpectralFlux = new double[_threshold.Length];

			for( int i = 0; i < _threshold.Length; i++ )
			{
				if( _threshold[i] <= _soundParser.SpectralFlux[i] )
				    prunnedSpectralFlux[i] = _soundParser.SpectralFlux[i] - _threshold[i];
				else
				    prunnedSpectralFlux[i] = 0;
			}

			for( int i = 0; i < prunnedSpectralFlux.Length - 1; i++ )
			{
			   if( prunnedSpectralFlux[i] > prunnedSpectralFlux[i+1] )
			      _peaks[i] = prunnedSpectralFlux[i];
			   else
			      _peaks[i] = 0;				
			}
		}

		/// <summary>
		/// Kalman's filter for smooth xy function. For our case filter will smooth flux thresholds function.
		/// More: http://en.wikipedia.org/wiki/Kalman_filter
		/// </summary>
		/// <param name="q">Measurement noise</param>
		/// <param name="r">Environment noise</param>
		/// <param name="f">Factor of real value to previous real value</param>
		/// <param name="h">Factor of measured value to real value</param>
		private void CalculateKalmanFilter(double q = .35, double r = 35, double f = 1, double h = 1)
		{
			double state = _threshold[0];
			double covariance = .1;
			
			for (int i = 0; i < _threshold.Length; i++)
			{
				double x0 = f * state;
				double p0 = f * covariance * f + q;
				double k = h * p0 / (h * p0 * h + r);
				state = x0 + k * (_threshold[i] - h * x0);
				covariance = (1 - k * h) * p0;
				_threshold[i] = state;
			}
		}

		/// <summary>
		/// Convert all flux thresholds values into 0..1 representation.
		/// </summary>
		private void ConvertPercents()
		{
			double maxFlux = _threshold[0];
			int i;
			for (i = 1; i < _threshold.Length; i++)
				maxFlux = (maxFlux < _threshold[i]) ? _threshold[i] : maxFlux;
			for (i = 0; i < _threshold.Length; i++)
				_threshold[i] = _threshold[i] / maxFlux;
		}

		private void CalculateSumOfThresholds()
		{
			_sumOfFluxThresholds = 0;
			for (int i = 0; i < _threshold.Length; i++)
				_sumOfFluxThresholds += _threshold[i];
		}

        public double[] Thresholds { get { return _threshold; } }

        public double[] Peaks { get { return _peaks; } }

        public double[,] Beats { get { return _soundParser.Beats; } }

        public double SpeedFactor { get { return _threshold.Length / _sumOfFluxThresholds; } }
    }
}
