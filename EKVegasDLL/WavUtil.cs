/*
 * Copyright (c) 2017 Evan Kale
 * Email: EvanKale91@gmail.com
 * Web: www.youtube.com/EvanKale
 * Social: @EvanKale91
 *
 * This file is part of VegasScripts.
 *
 * VegasScripts is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using ScriptPortal.Vegas;
using System.IO;

namespace EKVegas
{
    public class WavUtil
    {
        static string WAV_EXT = ".wav";
        static string WAV_RENDERER_NAME = "Wave (Microsoft)";
        static string WAV_RENDER_TEMPLATE_NAME = "Default Template";
        static string WAV_TEMP_SUFFIX = "_ekVegasTemp";
        static int WAV_EXPORT_SAMPLE_RATE = 48000;
        static int MAX_PULSE_LENGTH_MICROS = 300;

        Vegas vegas;
        Util util;

        public WavUtil(Vegas vegas, Util util)
        {
            this.vegas = vegas;
            this.util = util;
        }

        //Creates a temporary WAV file, exporting all audio in the span of the VideoEvent
        public string CreateVideoEventWAV(VideoEvent videoEvent)
        {
            string currentMediaPath = videoEvent.ActiveTake.MediaPath;
            string currentMediaPathWithoutExt = Path.ChangeExtension(currentMediaPath, null);

            Timecode eventStart = videoEvent.Start;
            Timecode eventLength = videoEvent.Length;

            string waveOutPath = currentMediaPathWithoutExt + WAV_TEMP_SUFFIX + WAV_EXT;

            Renderer renderer = util.FindRenderer(WAV_RENDERER_NAME);

            if (renderer == null)
            {
                util.ShowError("Renderer \"" + WAV_RENDERER_NAME + "\" not found");
                return null;
            }

            RenderTemplate renderTemplate = util.FindRenderTemplate(renderer, WAV_RENDER_TEMPLATE_NAME);

            if (renderTemplate == null)
            {
                util.ShowError("RenderTemplate \"" + WAV_RENDER_TEMPLATE_NAME + "\" not found");
                return null;
            }

            RenderArgs renderArgs = new RenderArgs();
            renderArgs.OutputFile = waveOutPath;
            renderArgs.RenderTemplate = renderTemplate;
            renderArgs.Start = eventStart;
            renderArgs.Length = eventLength;

            RenderStatus renderStatus = util.DoRender(renderArgs);

            if (renderStatus == RenderStatus.Complete)
            {
                return waveOutPath;
            }

            return null;
        }

        public enum EdgeType
        {
            NONE,
            RISE,
            FALL
        };

        public struct Blip
        {
            public double locationInMicroseconds;
            public int pulseCount;
        };

        public List<Blip> FindBlips(short[] data)
        {
            List<Blip> blips = new List<Blip>();

            int edgeThreshold = short.MaxValue / 3;

            double microsPerSample = (1.0f / WAV_EXPORT_SAMPLE_RATE) * 1000000;
            int maxSamplesBetweenPulses = (int)Math.Ceiling(MAX_PULSE_LENGTH_MICROS / microsPerSample);

            //Edge count data
            EdgeType prevEdgeType = EdgeType.NONE;
            int prevEdgeIndex = 0;
            int edgeCount = 0;

            EdgeType edgeType;

            for (int i = 1; i < data.Length; ++i)
            {
                edgeType = EdgeType.NONE;

                if ((i - prevEdgeIndex) > maxSamplesBetweenPulses)
                {
                    if (edgeCount > 1)
                    {
                        //Store blip data
                        Blip blip = new Blip();
                        blip.locationInMicroseconds = prevEdgeIndex * microsPerSample;
                        blip.pulseCount = edgeCount / 2;
                        blips.Add(blip);
                    }

                    //Reset edge count data
                    prevEdgeType = EdgeType.NONE;
                    prevEdgeIndex = 0;
                    edgeCount = 0;
                }

                //determine if current sample is an edge
                if (data[i] - data[i - 1] >= edgeThreshold)
                {
                    edgeType = EdgeType.RISE;
                }
                else if (data[i] - data[i - 1] <= (-edgeThreshold))
                {
                    edgeType = EdgeType.FALL;
                }

                //if current sample is a new edge then update edge count data
                if (edgeType != EdgeType.NONE && edgeType != prevEdgeType)
                {
                    prevEdgeType = edgeType;
                    prevEdgeIndex = i;
                    edgeCount++;
                }
            }

            return blips;
        }

        public void SplitAtBlips(TrackEvent trackEvent, List<Blip> blips)
        {
            TrackEvent currTrackEvent = trackEvent;
            foreach (Blip blip in blips)
            {
                double blipLocation = trackEvent.Start.ToMilliseconds() + (blip.locationInMicroseconds / 1000);
                Timecode timecode = new Timecode(blipLocation);
                currTrackEvent = util.Split(currTrackEvent, timecode, true);
            }
        }

        public bool ReadWav(string fileName, out short[] leftChannel, out short[] rightChannel)
        {
            leftChannel = null;
            rightChannel = null;

            FileStream fileStream = null;
            BinaryReader binaryReader = null;

            try
            {
                fileStream = File.Open(fileName, FileMode.Open);
                binaryReader = new BinaryReader(fileStream);

                // WAV Structure:
                //http://soundfile.sapp.org/doc/WaveFormat/

                // RIFF
                int chunkID = binaryReader.ReadInt32();         //"RIFF"
                int chunkSize = binaryReader.ReadInt32();       //Size of entire file minus 8 bytes
                int format = binaryReader.ReadInt32();          //"WAVE"

                // FMT
                int subchunk1ID = binaryReader.ReadInt32();     //"fmt "
                int subchunk1Size = binaryReader.ReadInt32();   //16 for PCM (size of FMT chunk minus 8 bytes)
                int audioFormat = binaryReader.ReadInt16();     //1 for PCM
                int numChannels = binaryReader.ReadInt16();     //Stereo = 2
                int sampleRate = binaryReader.ReadInt32();      //48000 (Hz)
                int byteRate = binaryReader.ReadInt32();        //SampleRate * NumChannels * BitsPerSample/8
                int blockAlign = binaryReader.ReadInt16();      //NumChannels * BitsPerSample/8
                int bitsPerSample = binaryReader.ReadInt16();   //16 (bits per sample)

                if (subchunk1Size != 16
                    || audioFormat != 1
                    || numChannels != 2
                    || sampleRate != WAV_EXPORT_SAMPLE_RATE
                    || bitsPerSample != 16)
                {
                    util.ShowError("Improper WAV export");
                    return false;
                }

                // DATA
                int subchunk2ID = binaryReader.ReadInt32();
                int subchunk2Size = binaryReader.ReadInt32();   //size of data chunk minus 8 bytes

                int numSamples = subchunk2Size / 4;
                leftChannel = new short[numSamples];
                rightChannel = new short[numSamples];

                for (int i = 0; i < numSamples; ++i)                 //read the audio data
                {
                    leftChannel[i] = binaryReader.ReadInt16();
                    rightChannel[i] = binaryReader.ReadInt16();
                }

            }
            catch (Exception e)
            {
                util.ShowError(e.ToString());
                return false;
            }
            finally
            {
                if (binaryReader != null)
                    binaryReader.Close();
            }

            return true;
        }
    }
}
