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

// Maps all blips (created by the Blipper) to MIDI notes of an input MIDI file.

using ScriptPortal.Vegas;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EKVegas;
using NAudio.Midi;

public class EntryPoint
{
    //Input MIDI file
    const string midiFilePath = "C:/songs/drmario.mid";
    //Which layer to read the MIDI notes from
    const int midiReadLayer = 1; //BASS in drmario.mid
    //const int midiReadLayer = 2; //MELODY in drmario.mid
    //const int midiReadLayer = 10; //DRUMS in drmario.mid
    //Offset the blip starting point (ms)
    const double blipStartOffset = 100;
    //Extend the note's duration (ms)
    const double noteEndOffset = 0;
    //Fade in duration (ms) of the created audio events
    const double fadeInLength = 10;
    //Fade out duration (ms) of the created audio events
    const double fadeOutLength = 15;

    //A map between note names and blip indices
    Dictionary<string, int> pitchBlipMap = new Dictionary<string, int>
    {
        //bass
        {"G3", 12},
        {"E3", 13},
        {"G#3", 14},
        {"A3", 15},
        {"C4", 16},
        {"D4", 17},
        {"D#4", 18},
        //tenor
        {"E5", 2},
        {"A5", 3},
        {"C6", 4},
        {"D6", 5},
        {"E6", 6},
        {"F6", 7},
        {"F#6", 8},
        {"G6", 9},
        {"A6", 10},
        //soprano
        {"E7", 26},
        {"A7", 25},
        {"C8", 24},
        {"C#8", 23},
        {"D8", 22},
        //beatbox
        {"Bass Drum 1", 1},
        {"Acoustic Snare", 19},
    };

    Vegas vegas = null;
    Util util = null;
    WavUtil wavUtil = null;

    List<WavUtil.Blip> blips;
    VideoEvent selectedVideoEvent;
    AudioEvent selectedAudioEvent;

    public void FromVegas(Vegas vegas)
    {
        this.vegas = vegas;
        this.util = new Util(vegas);
        this.wavUtil = new WavUtil(vegas, util);

        ProcessBlips();
        ProcessMidi();
    }

    void ProcessBlips()
    {
        selectedVideoEvent = util.GetFirstSelectedVideoEvent();

        if (selectedVideoEvent == null)
        {
            util.ShowError("No video event selected");
            return;
        }

        TrackEventGroup selectedVideoEventGroup = selectedVideoEvent.Group;

        //take the first audio track in the selected video's group
        foreach (TrackEvent trackEventInGroup in selectedVideoEventGroup)
        {
            if (trackEventInGroup is AudioEvent)
            {
                selectedAudioEvent = trackEventInGroup as AudioEvent;
                break;
            }
        }

        if (selectedAudioEvent == null)
        {
            util.ShowError("Selected video event has no audio event in its group");
            return;
        }

        //Create a temporary WAV file, and export the audio span of selected video event
        string wavePath = wavUtil.CreateVideoEventWAV(selectedVideoEvent);

        if (wavePath == null)
        {
            util.ShowError("Unable to export temporary WAV");
            return;
        }

        short[] leftChannel, rightChannel;
        bool wavReadStatus = wavUtil.ReadWav(wavePath, out leftChannel, out rightChannel);

        if (wavReadStatus == false)
        {
            util.ShowError("Unable to read WAV export file.");
            return;
        }

        //Delete the temporary file
        File.Delete(wavePath);

        //Find all blips in the left channel and split tracks at blips
        blips = wavUtil.FindBlips(rightChannel);
    }

    int GetBlipFromPitch(string pitch)
    {
        if (pitchBlipMap.ContainsKey(pitch))
            return pitchBlipMap[pitch];
        else return -1;
    }

    void ProcessMidi()
    {
        VideoTrack destVideoTrack = new VideoTrack(0, "destVideoTrack");
        AudioTrack destAudioTrack = new AudioTrack(0, "destAudioTrack");
        vegas.Project.Tracks.Add(destAudioTrack);
        vegas.Project.Tracks.Add(destVideoTrack);

        MidiFile midi = new MidiFile(midiFilePath);
        Dictionary<string, double> onNotes = new Dictionary<string, double>();

        TempoEvent lastTempoEvent = new TempoEvent(0, 0);
        double lastTempoEventTime = 0;

        for (int i = 0; i < midi.Events.Count(); i++)
        {
            foreach (MidiEvent midiEvent in midi.Events[i])
            {
                if (midiEvent is TempoEvent)
                {
                    lastTempoEvent = (midiEvent as TempoEvent);
                    lastTempoEventTime = ((double)(midiEvent.AbsoluteTime - lastTempoEvent.AbsoluteTime) / midi.DeltaTicksPerQuarterNote) * lastTempoEvent.MicrosecondsPerQuarterNote + lastTempoEventTime;
                }
                else if (midiEvent is NoteEvent && midiEvent.Channel == midiReadLayer)
                {
                    NoteEvent noteEvent = midiEvent as NoteEvent;
                    double usTime = ((double)(noteEvent.AbsoluteTime - lastTempoEvent.AbsoluteTime) / midi.DeltaTicksPerQuarterNote) * lastTempoEvent.MicrosecondsPerQuarterNote + lastTempoEventTime;
                    double msTime = usTime / 1000;
                    int velocity = noteEvent.Velocity;

                    if (noteEvent.CommandCode == MidiCommandCode.NoteOn)
                    {
                        onNotes[noteEvent.NoteName] = msTime;
                    }
                    else if (noteEvent.CommandCode == MidiCommandCode.NoteOff)
                    {
                        if (onNotes.ContainsKey(noteEvent.NoteName))
                        {
                            
                            double noteStartTime = onNotes[noteEvent.NoteName];
                            double noteEndTime = msTime;
                            int blipNum = GetBlipFromPitch(noteEvent.NoteName);

                            if(blipNum == -1)
                            {
                                //note not found in pitchBlipMap
                                continue;
                            }

                            onNotes.Remove(noteEvent.NoteName);

                            //Map the MIDI note to a video/audio event
                            VideoEvent videoEvent = selectedVideoEvent.Copy(destVideoTrack, selectedVideoEvent.Start) as VideoEvent;
                            AudioEvent audioEvent = selectedAudioEvent.Copy(destAudioTrack, selectedAudioEvent.Start) as AudioEvent;

                            Timecode clipDurationMillis = new Timecode(noteEndTime - noteStartTime + noteEndOffset);
                            Timecode globalClipStartLocation = new Timecode(selectedVideoEvent.Start.ToMilliseconds() + (blips[blipNum].locationInMicroseconds / 1000) + blipStartOffset);

                            TrackEvent tmpTrackEvent = videoEvent;
                            util.TrimEvent(ref tmpTrackEvent, globalClipStartLocation, clipDurationMillis);
                            videoEvent = tmpTrackEvent as VideoEvent;

                            tmpTrackEvent = audioEvent;
                            util.TrimEvent(ref tmpTrackEvent, globalClipStartLocation, clipDurationMillis);
                            audioEvent = tmpTrackEvent as AudioEvent;
                            audioEvent.FadeIn.Length = new Timecode(fadeInLength);
                            audioEvent.FadeOut.Length = new Timecode(fadeOutLength);

                            videoEvent.Start = new Timecode(noteStartTime);
                            audioEvent.Start = new Timecode(noteStartTime);
                        }
                    }
                }
            }

        }
    }
}