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

//Creates splits where blips are made in the left channel audio
// during the span of the selected video track.

using ScriptPortal.Vegas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

public class EntryPoint
{
    static string WAV_EXT = ".wav";
    static string WAV_RENDERER_NAME = "Wave (Microsoft)";
    static string WAV_RENDER_TEMPLATE_NAME = "Default Template";
    static string WAV_TEMP_SUFFIX = "_blipperTemp";
    static int WAV_EXPORT_SAMPLE_RATE = 48000;
    static int MAX_PULSE_LENGTH_MICROS = 300;

    Vegas vegas = null;

    public void FromVegas(Vegas vegas)
    {
        this.vegas = vegas;

        VideoEvent videoEvent = GetFirstSelectedVideoEvent();

        if (videoEvent == null)
        {
            ShowError("No video event selected");
            return;
        }

        //Create a temporary WAV file, and export the audio span of selected video event
        string wavePath = CreateVideoEventWAV(videoEvent);

        if(wavePath == null)
        {
            ShowError("Unable to export temporary WAV");
            return;
        }

        short[] leftChannel, rightChannel;
        bool wavReadStatus = ReadWav(wavePath, out leftChannel, out rightChannel);

        if (wavReadStatus == false)
        {
            ShowError("Unable to read WAV export file.");
            return;
        }

        //Delete the temporary file
        File.Delete(wavePath);

        //Find all blips in the left channel and split tracks at blips
        List<Blip> blips = FindBlips(leftChannel);
        SplitAtBlips(videoEvent, blips);
    }

    public VideoEvent GetFirstSelectedVideoEvent()
    {
        foreach (Track track in vegas.Project.Tracks)
        {
            if (track.IsValid() && track.IsVideo())
            {
                foreach (TrackEvent trackEvent in track.Events)
                {
                    if (trackEvent.IsVideo() && trackEvent.Selected)
                    {
                        return (VideoEvent)trackEvent;
                    }
                }
            }
        }
        return null;
    }

    //Creates a temporary WAV file, exporting all audio in the span of the VideoEvent
    string CreateVideoEventWAV(VideoEvent videoEvent)
    {
        string currentMediaPath = videoEvent.ActiveTake.MediaPath;
        string currentMediaPathWithoutExt = Path.ChangeExtension(currentMediaPath, null);

        Timecode eventStart = videoEvent.Start;
        Timecode eventLength = videoEvent.Length;

        string waveOutPath = currentMediaPathWithoutExt + WAV_TEMP_SUFFIX + WAV_EXT;

        Renderer renderer = FindRenderer(WAV_RENDERER_NAME);

        if (renderer == null)
        {
            ShowError("Renderer \"" + WAV_RENDERER_NAME + "\" not found");
            return null;
        }

        RenderTemplate renderTemplate = FindRenderTemplate(renderer, WAV_RENDER_TEMPLATE_NAME);

        if (renderTemplate == null)
        {
            ShowError("RenderTemplate \"" + WAV_RENDER_TEMPLATE_NAME + "\" not found");
            return null;
        }

        RenderArgs renderArgs = new RenderArgs();
        renderArgs.OutputFile = waveOutPath;
        renderArgs.RenderTemplate = renderTemplate;
        renderArgs.Start = eventStart;
        renderArgs.Length = eventLength;

        RenderStatus renderStatus = DoRender(renderArgs);

        if(renderStatus == RenderStatus.Complete)
        {
            return waveOutPath;
        }

        return null;
    }

    Renderer FindRenderer(string findRendererName)
    {
        foreach(Renderer renderer in vegas.Renderers)
        {
            if(renderer.FileTypeName.Equals(findRendererName, StringComparison.OrdinalIgnoreCase))
            {
                return renderer;
            }
        }
        return null;
    }

    RenderTemplate FindRenderTemplate(Renderer renderer, string findRenderTemplateName)
    {
        foreach (RenderTemplate renderTemplate in renderer.Templates)
        {
            if (renderTemplate.Name.Equals(findRenderTemplateName, StringComparison.OrdinalIgnoreCase))
            {
                return renderTemplate;
            }
        }
        return null;
    }

    RenderStatus DoRender(RenderArgs args)
    {
        RenderStatus status = vegas.Render(args);
        switch (status)
        {
            case RenderStatus.Complete:
            case RenderStatus.Canceled:
                break;
            case RenderStatus.Failed:
            default:
                ShowError("Render failed:\n"
                    + "\n    File name: "
                    + args.OutputFile
                    + "\n    Template: "
                    + args.RenderTemplate.Name);
                break;
        }
        return status;
    }

    enum EdgeType
    {
        NONE,
        RISE,
        FALL
    };

    struct Blip
    {
        public double locationInMicroseconds;
        public int pulseCount;
    };

    List<Blip> FindBlips(short[] data)
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

    void SplitAtBlips(TrackEvent trackEvent, List<Blip> blips)
    {
        TrackEvent currTrackEvent = trackEvent;
        foreach (Blip blip in blips)
        {
            double blipLocation = trackEvent.Start.ToMilliseconds() + (blip.locationInMicroseconds / 1000);
            Timecode timecode = new Timecode(blipLocation);
            currTrackEvent = Split(currTrackEvent, timecode, true);
        }
    }

    //Split a track event at a global location, and creating new groups if splitGroups = true
    TrackEvent Split(TrackEvent trackEvent, Timecode splitLocation, bool splitGroups)
    {
        TrackEventGroup trackEventGroup = trackEvent.Group;

        //If not splitting groups or not in a group, then perform split on event and return
        if (!splitGroups || trackEventGroup == null)
        {
            return trackEvent.Split(splitLocation - trackEvent.Start);
        }

        //Create new group to hold split events
        TrackEventGroup splitGroup = new TrackEventGroup();
        vegas.Project.Groups.Add(splitGroup);

        //Reference to splitEvent of trackEvent
        TrackEvent returnSplitEvent = null;

        //Split every track event in the group
        foreach (TrackEvent trackEventInGroup in trackEventGroup)
        {
            //If splitLocation splits the track, then split it
            if (isTimecodeInTrackEvent(trackEventInGroup, splitLocation))
            {
                TrackEvent splitEvent = trackEventInGroup.Split(splitLocation - trackEventInGroup.Start);

                //remove the splitEvent from group, and add it to splitGroup
                trackEventGroup.Remove(splitEvent);
                splitGroup.Add(splitEvent);

                //if this track is the trackEvent in the parameter, then return it's split event
                if (trackEventInGroup == trackEvent)
                {
                    returnSplitEvent = splitEvent;
                }
            }
            //If splitLocation comes before the track, then move the track to splitGroup
            else if (isTimecodeBeforeTrackEvent(trackEventInGroup, splitLocation))
            {
                //remove the trackEventInGroup from group, and add it to splitGroup
                trackEventGroup.Remove(trackEventInGroup);
                splitGroup.Add(trackEventInGroup);
            }
        }

        return returnSplitEvent;
    }

    //Split all tracks at a global location
    void Split(Timecode splitLocation)
    {
        foreach (Track track in vegas.Project.Tracks)
        {
            Split(track, splitLocation);
        }
    }

    //Split a track at a global location
    void Split(Track track, Timecode splitLocation)
    {
        foreach (TrackEvent trackEvent in track.Events)
        {
            if (isTimecodeInTrackEvent(trackEvent, splitLocation))
            {
                trackEvent.Split(splitLocation - trackEvent.Start);
            }
        }
    }

    bool isTimecodeInTrackEvent(TrackEvent trackEvent, Timecode timecode)
    {
        return (trackEvent.Start <= timecode
                && (trackEvent.Start + trackEvent.Length) >= timecode);
    }

    bool isTimecodeBeforeTrackEvent(TrackEvent trackEvent, Timecode timecode)
    {
        return (trackEvent.Start + trackEvent.Length) >= timecode;
    }

    bool isTimecodeAfterTrackEvent(TrackEvent trackEvent, Timecode timecode)
    {
        return trackEvent.Start <= timecode;
    }

    bool ReadWav(string fileName, out short[] leftChannel, out short[] rightChannel)
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
                ShowError("Improper WAV export");
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
            ShowError(e.ToString());
            return false;
        }
        finally
        {
            if (binaryReader != null)
                binaryReader.Close();
        }

        return true;
    }

    void ShowError(string error)
    {
        MessageBox.Show(error);
    }
}