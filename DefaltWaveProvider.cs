using System;
using NAudio.Dmo.Effect;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Defalt {
    public class DefaltSampleProvider : ISampleProvider {
        private const float PitchStepsPerSecond = 30.0f;
        private const float MinShiftDelay = 0.5f;
        private const float MaxShiftDelay = 0.8f;
        private const float VolumeSlice = 0.1f;
        //private const float VolThreshold = 0.4f;
        private const float VolBase = 0.4f;
        private const float VolRange = 0.2f;

        //private const float ReverseMax = 0.2f;
        private readonly ISampleProvider _sourceSampleProvider;

        private readonly SmbPitchShiftingSampleProvider _shifter;

        //private readonly int _reverseStep;
        private readonly int _pitchStep;
        private readonly int _minShiftDelay;
        private readonly int _rangeShiftDelay;
        private readonly Random _r;
        private readonly float _volThreshold;

        private int _shiftTimer;
        private float _volShift;

        public DefaltSampleProvider(ISampleProvider sourceSampleProvider, float threshold) {
            _shifter = new SmbPitchShiftingSampleProvider(sourceSampleProvider,
                512, 10, 0.8f);
            //_reverseStep = (int) (ReverseMax * sourceWaveProvider.WaveFormat.SampleRate);
            _pitchStep = Math.Max(1, (int) (sourceSampleProvider.WaveFormat.SampleRate / PitchStepsPerSecond));
            _minShiftDelay = (int) (MinShiftDelay * sourceSampleProvider.WaveFormat.SampleRate);
            _rangeShiftDelay = (int) ((MaxShiftDelay - MinShiftDelay) * sourceSampleProvider.WaveFormat.SampleRate);
            _volThreshold = threshold;
            _r = new Random();

            var altSrc = new MeteringSampleProvider(_shifter);
            altSrc.SamplesPerNotification = (int) (VolumeSlice * sourceSampleProvider.WaveFormat.SampleRate);
            altSrc.StreamVolume += (sender, args) => { _volShift = args.MaxSampleValues[0]; };
            var subSrc = new SampleToWaveProvider(altSrc);

            var srcChorus = new DmoEffectWaveProvider<DmoChorus, DmoChorus.Params>(subSrc);
            var chorus = srcChorus.EffectParams;
            chorus.Frequency = 0.3f;
            chorus.WaveForm = ChorusWaveForm.Sin;
            chorus.Delay = 200.0f;
            chorus.FeedBack = 50.0f;
            chorus.Depth = 20.0f;
            chorus.Phase = ChorusPhase.Neg90;
            chorus.WetDryMix = 50.0f;
            var srcChorus2 = new DmoEffectWaveProvider<DmoChorus, DmoChorus.Params>(srcChorus);
            var chorus2 = srcChorus2.EffectParams;
            chorus2.Frequency = 0.3f;
            chorus2.WaveForm = ChorusWaveForm.Sin;
            chorus2.Delay = 400.0f;
            chorus2.FeedBack = 50.0f;
            chorus2.Depth = 20.0f;
            chorus2.Phase = ChorusPhase.Neg180;
            chorus2.WetDryMix = 70.0f;
            var srcDistortion = new DmoEffectWaveProvider<DmoDistortion, DmoDistortion.Params>(srcChorus2);
            var distortion = srcDistortion.EffectParams;
            distortion.Gain = -10.0f;
            distortion.Edge = 20.0f;
            distortion.PostEqCenterFrequency = 5000.0f;
            distortion.PostEqBandWidth = 700.0f;
            distortion.PreLowPassCutoff = 6000.0f;
            /*var srcGargle = new DmoEffectWaveProvider<DmoGargle, DmoGargle.Params>(srcDistortion);
            var gargle = srcGargle.EffectParams;
            gargle.RateHz = 800;
            gargle.WaveShape = GargleWaveShape.Square;*/
            //_sourceSampleProvider = new WaveToSampleProvider(srcGargle);
            _sourceSampleProvider = new WaveToSampleProvider(srcDistortion);
        }

        public int Read(float[] buffer, int offset, int count) {
            int read;
            var shiftSource = _shifter.PitchFactor;
            float shiftTarget;
            _shiftTimer -= count;
            if (_shiftTimer <= 0) {
                _shiftTimer = _minShiftDelay + (int) (_r.NextDouble() * _rangeShiftDelay);
                if (_r.NextDouble() < 0.5)
                    shiftTarget = (float) (0.7f + _r.NextDouble() * 0.1f);
                else
                    shiftTarget = (float) (1.2f + _r.NextDouble() * 0.1f);
            }
            else
                shiftTarget = shiftSource;

            if (_volShift > _volThreshold)
                shiftTarget = (float) (1.0f + VolBase + _r.NextDouble() * VolRange);

            shiftTarget -= shiftSource;
            read = 0;
            int readI;
            for (var i = 0; i < count && read < count; i += readI) {
                _shifter.PitchFactor = shiftSource + shiftTarget * ((float) i / count);
                readI = _sourceSampleProvider.Read(buffer, offset + i, Math.Min(_pitchStep, count - read));
                read += readI;
                if (readI == 0)
                    break;
            }

            _shifter.PitchFactor = shiftSource + shiftTarget;
            //var e = offset + read;

            // if (r < 0.2) {
            //     for (var i = 0; i <= e - _reverseStep; i += _reverseStep)
            //         buffer.AsSpan(offset + i, _reverseStep).Reverse();
            // }


            return read;
        }

        public WaveFormat WaveFormat => _sourceSampleProvider.WaveFormat;
    }
}