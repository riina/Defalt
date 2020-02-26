using NAudio.Wave;

namespace Defalt {
    internal class Instance {
        private IWaveIn _recorder;
        private BufferedWaveProvider _recorderWaveProvider;
        private ISampleProvider _baseWaveProvider;
        private DefaltSampleProvider _defaltSampleProvider;
        private SavingWaveProvider _savingWaveProvider;
        private WaveOutEvent _player;

        internal void Start(string path, float threshold, int device, float bufferTime, string outputPath) {
            if (path != null) {
                _baseWaveProvider =  new WaveFileReader(path).ToSampleProvider();
            }
            else {
                _recorder = new WaveInEvent {
                    DeviceNumber = device,
                    BufferMilliseconds = (int) (bufferTime * 1000.0f),
                    WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, WaveInEvent.GetCapabilities(0).Channels)
                };
                _recorder.DataAvailable += RecorderOnDataAvailable;
            _recorderWaveProvider = new BufferedWaveProvider(_recorder.WaveFormat);
            _baseWaveProvider = _recorderWaveProvider.ToSampleProvider();
            }

            _defaltSampleProvider = new DefaltSampleProvider(_baseWaveProvider, threshold);
            _savingWaveProvider = new SavingWaveProvider(_defaltSampleProvider.ToWaveProvider(),  outputPath);

            _player = new WaveOutEvent();
            _player.Init(_savingWaveProvider);

            _player.Play();
            _recorder?.StartRecording();
        }

        private void RecorderOnDataAvailable(object sender, WaveInEventArgs waveInEventArgs) =>
            _recorderWaveProvider.AddSamples(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);

        internal void Stop() {
            _player.Stop();
            _recorder = null;
            _recorderWaveProvider = null;
            _baseWaveProvider = null;
            _defaltSampleProvider = null;
            _savingWaveProvider = null;
            _player = null;
        }
    }
}