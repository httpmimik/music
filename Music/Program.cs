using System;
using System.IO;
using NAudio.Wave;
using NAudio.Dsp;

namespace MusicVisualizer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("Enter the path to the audio file (e.g., .mp3, .wav):");
            string filePath = Console.ReadLine();

            if (!File.Exists(filePath))
            {
                Console.WriteLine("File not found. Exiting...");
                return;
            }

            Console.Clear();
            Console.WriteLine("Music Visualizer (Press Ctrl+C to exit)");

            using (var reader = new AudioFileReader(filePath))
            using (var waveOut = new WaveOutEvent())
            {
                waveOut.Init(reader);
                waveOut.Play();

                int fftLength = 2048; // Number of samples for FFT (power of 2)
                var fftBuffer = new Complex[fftLength];
                var audioBuffer = new float[fftLength];
                DateTime nextFrame = DateTime.Now;

                try
                {
                    while (waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        // Debug playback position
                        Console.WriteLine($"Playback Time: {reader.CurrentTime.TotalMilliseconds}ms");

                        // Wait for the next frame (every 50ms)
                        if (DateTime.Now < nextFrame)
                        {
                            System.Threading.Thread.Sleep(1);
                            continue;
                        }
                        nextFrame = DateTime.Now.AddMilliseconds(50); // 20 FPS

                        int samplesRead = reader.Read(audioBuffer, 0, fftLength);

                        if (samplesRead == 0) break; // End of audio

                        // Fill FFT buffer with audio samples
                        for (int i = 0; i < fftLength; i++)
                        {
                            fftBuffer[i] = i < samplesRead
                                ? new Complex { X = audioBuffer[i], Y = 0 }
                                : new Complex { X = 0, Y = 0 };
                        }

                        // Perform FFT
                        FastFourierTransform.FFT(true, (int)Math.Log(fftLength, 2), fftBuffer);

                        // Debug FFT magnitudes
                        for (int i = 0; i < 10; i++) // Log first 10 bins
                        {
                            double magnitude = Math.Sqrt(fftBuffer[i].X * fftBuffer[i].X + fftBuffer[i].Y * fftBuffer[i].Y);
                            Console.WriteLine($"FFT[{i}]: {magnitude}");
                        }

                        // Render the visualizer
                        RenderVisualizer(fftBuffer);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        static void RenderVisualizer(Complex[] fftBuffer)
        {
            Console.Clear();

            int numBars = 64; // Number of bars to display
            int barHeightScale = 500; // Adjust sensitivity

            for (int i = 0; i < numBars; i++)
            {
                // Map FFT bins to visualizer bars
                int index = i * (fftBuffer.Length / numBars);
                double magnitude = Math.Sqrt(fftBuffer[index].X * fftBuffer[index].X + fftBuffer[index].Y * fftBuffer[index].Y);
                int barHeight = (int)(magnitude * barHeightScale);

                // Cap bar height to console height
                barHeight = Math.Min(barHeight, Console.WindowHeight - 1);

                // Render the bar as ASCII
                if (barHeight > 0)
                {
                    Console.WriteLine(new string('█', barHeight));
                }
                else
                {
                    Console.WriteLine(" ");
                }
            }
        }
    }
}
