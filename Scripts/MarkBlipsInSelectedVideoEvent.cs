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

// Finds all blips of a selected video event(inserted by the Blipper) and adds a bookmark at that location.
// Hit 'solo' on the audio track of the video event before running this script.

using ScriptPortal.Vegas;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EKVegas;
using NAudio.Midi;

public class EntryPoint
{
    Vegas vegas = null;
    Util util = null;
    WavUtil wavUtil = null;

    List<WavUtil.Blip> blips;
    VideoEvent selectedVideoEvent;

    public void FromVegas(Vegas vegas)
    {
        this.vegas = vegas;
        this.util = new Util(vegas);
        this.wavUtil = new WavUtil(vegas, util);

        processBlips();

        foreach (WavUtil.Blip blip in blips)
        {
            vegas.Project.Markers.Add(new Marker(new Timecode(blip.locationInMicroseconds/1000) + selectedVideoEvent.Start, blip.pulseCount+""));
        }
    }

    void processBlips()
    {
        selectedVideoEvent = util.GetFirstSelectedVideoEvent();

        if (selectedVideoEvent == null)
        {
            util.ShowError("No video event selected");
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

        //Find all blips in the right channel and split tracks at blips
        blips = wavUtil.FindBlips(rightChannel);
    }

}