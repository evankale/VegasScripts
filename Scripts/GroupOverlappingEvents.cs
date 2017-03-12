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

// Groups overlapping selected track events.
// Requires 1 track to be selected that contains selected track events.

using ScriptPortal.Vegas;
using EKVegas;
using System.Collections.Generic;
using System.Collections;

public class EntryPoint
{
    Util util;
    Vegas vegas;

    public void FromVegas(Vegas vegas)
    {
        this.vegas = vegas;
        util = new Util(vegas);

        Dictionary<Track, List<TrackEvent>> selectedTrackEventMap;
        Dictionary<Track, List<TrackEvent>> nonSelectedtrackEventMap;
        util.GetSelectedTrackEvents(out selectedTrackEventMap, out nonSelectedtrackEventMap);

        if (selectedTrackEventMap.Count != 1)
        {
            util.ShowError("Requires: 1 selected track with selected events");
            return;
        }

        //Assuming there's only 1 selected track, then get the selected track like this:
        IEnumerator selectedTrackEnum = selectedTrackEventMap.Keys.GetEnumerator();
        selectedTrackEnum.MoveNext();
        Track selectedTrack = (Track)selectedTrackEnum.Current;

        if(selectedTrack == null)
        {
            util.ShowError(selectedTrackEventMap.Count+"");
            return;
        }

        //Create a group for each selected track event in selected track
        Dictionary<TrackEvent, TrackEventGroup> selectedTrackEventGroups = new Dictionary<TrackEvent, TrackEventGroup>();
        foreach (TrackEvent trackEvent in selectedTrackEventMap[selectedTrack])
        {
            //Create new group to hold track event
            TrackEventGroup trackEventGroup = new TrackEventGroup();
            vegas.Project.Groups.Add(trackEventGroup);
            trackEventGroup.Add(trackEvent);
            selectedTrackEventGroups.Add(trackEvent, trackEventGroup);
        }

        //Go through every selected track event in non selected tracks and put them in the right groups (if possible)
        foreach (KeyValuePair<Track, List<TrackEvent>> nonSelectedtrackEventMapEntry in nonSelectedtrackEventMap)
        {
            foreach(TrackEvent trackEvent in nonSelectedtrackEventMapEntry.Value)
            {
                TrackEvent overlappingTrackEvent = util.GetOverlappingTrackEvent(trackEvent, new List<TrackEvent>(selectedTrackEventGroups.Keys));
                if(overlappingTrackEvent != null)
                    selectedTrackEventGroups[overlappingTrackEvent].Add(trackEvent);
            }
        }
    }

}