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
using ScriptPortal.Vegas;
using System.Windows.Forms;
using System.Collections.Generic;

namespace EKVegas
{
    public class Util
    {
        Vegas vegas;

        public Util(Vegas vegas)
        {
            this.vegas = vegas;
        }

        public bool IsTimecodeInTrackEvent(TrackEvent trackEvent, Timecode timecode, bool exclusive = false)
        {
            if (exclusive)
                return (trackEvent.Start < timecode
                    && (trackEvent.Start + trackEvent.Length) > timecode);
            return (trackEvent.Start <= timecode
                && (trackEvent.Start + trackEvent.Length) >= timecode);
        }

        public bool IsTimecodeBeforeTrackEvent(TrackEvent trackEvent, Timecode timecode, bool exclusive = false)
        {
            if (exclusive)
                return (trackEvent.Start + trackEvent.Length) > timecode;
            return (trackEvent.Start + trackEvent.Length) >= timecode;
        }

        public bool IsTimecodeAfterTrackEvent(TrackEvent trackEvent, Timecode timecode, bool exclusive = false)
        {
            if (exclusive)
                return trackEvent.Start < timecode;
            return trackEvent.Start <= timecode;
        }

        public bool IsTimecodeInTimecodeRegion(Timecode timecode, Timecode start, Timecode end, bool exclusive = false)
        {
            if (exclusive)
                return (start < timecode && end > timecode);
            return (start <= timecode && end >= timecode);
        }

        public bool IsEventInTimecodeRegion(TrackEvent trackEvent, Timecode start, Timecode end)
        {
            return IsTimecodeInTimecodeRegion(trackEvent.Start, start, end, true)
                || IsTimecodeInTimecodeRegion(trackEvent.End, start, end, true)
                || (start > trackEvent.Start && end < trackEvent.End);
        }

        public void ShowError(string error)
        {
            MessageBox.Show(error);
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

        public Renderer FindRenderer(string findRendererName)
        {
            foreach (Renderer renderer in vegas.Renderers)
            {
                if (renderer.FileTypeName.Equals(findRendererName, StringComparison.OrdinalIgnoreCase))
                {
                    return renderer;
                }
            }
            return null;
        }

        public RenderTemplate FindRenderTemplate(Renderer renderer, string findRenderTemplateName)
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

        public RenderStatus DoRender(RenderArgs args)
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

        //Split a track event at a global location, and creating new groups if splitGroups = true
        public TrackEvent Split(TrackEvent trackEvent, Timecode splitLocation, bool splitGroups)
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
                if (IsTimecodeInTrackEvent(trackEventInGroup, splitLocation))
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
                else if (IsTimecodeBeforeTrackEvent(trackEventInGroup, splitLocation))
                {
                    //remove the trackEventInGroup from group, and add it to splitGroup
                    trackEventGroup.Remove(trackEventInGroup);
                    splitGroup.Add(trackEventInGroup);
                }
            }

            return returnSplitEvent;
        }

        //Split all tracks at a global location
        public void Split(Timecode splitLocation)
        {
            foreach (Track track in vegas.Project.Tracks)
            {
                Split(track, splitLocation);
            }
        }

        //Split a track at a global location
        public void Split(Track track, Timecode splitLocation)
        {
            foreach (TrackEvent trackEvent in track.Events)
            {
                if (IsTimecodeInTrackEvent(trackEvent, splitLocation))
                {
                    trackEvent.Split(splitLocation - trackEvent.Start);
                }
            }
        }

        //Return track event within list with most overlap to trackEvent (if any)
        public TrackEvent GetOverlappingTrackEvent(TrackEvent trackEvent, List<TrackEvent> compareTrackEvents)
        {
            TrackEvent retEvent = null;

            double biggestOverlapPercentage = 0;

            foreach (TrackEvent compareTrackEvent in compareTrackEvents)
            {
                double overlapPercentage = GetTrackEventOverlapPercentage(trackEvent, compareTrackEvent);
                if (overlapPercentage > biggestOverlapPercentage)
                {
                    retEvent = compareTrackEvent;
                    biggestOverlapPercentage = overlapPercentage;
                }
            }

            return retEvent;
        }

        //Returns percentage of trackEvent timeline that is within in compareTrackEvent timeline
        //Returns [0.0 , 1.0]
        public double GetTrackEventOverlapPercentage(TrackEvent trackEvent, TrackEvent compareTrackEvent)
        {
            //overlap start = later of the two starts
            Timecode overlapStart = (trackEvent.Start > compareTrackEvent.Start) ? trackEvent.Start : compareTrackEvent.Start;

            //overlap end = earlier of the two ends
            Timecode overlapEnd = (trackEvent.End < compareTrackEvent.End) ? trackEvent.End : compareTrackEvent.End;

            if (overlapEnd <= overlapStart)
            {
                //then no overlap
                return 0;
            }

            double overlapDuration = overlapEnd.ToMilliseconds() - overlapStart.ToMilliseconds();
            double trackEventDuration = trackEvent.End.ToMilliseconds() - trackEvent.Start.ToMilliseconds();

            return overlapDuration / trackEventDuration;
        }

        public void GetSelectedTrackEvents(out Dictionary<Track, List<TrackEvent>> selectedTrackEventMap, out Dictionary<Track, List<TrackEvent>> nonSelectedtrackEventMap)
        {
            selectedTrackEventMap = new Dictionary<Track, List<TrackEvent>>();
            nonSelectedtrackEventMap = new Dictionary<Track, List<TrackEvent>>();

            foreach (Track track in vegas.Project.Tracks)
            {
                if (track.IsValid())
                {
                    foreach (TrackEvent trackEvent in track.Events)
                    {
                        if (trackEvent.IsValid() && trackEvent.Selected)
                        {
                            Dictionary<Track, List<TrackEvent>> mapToUse = null;
                            if (track.Selected)
                                mapToUse = selectedTrackEventMap;
                            else
                                mapToUse = nonSelectedtrackEventMap;

                            if (!mapToUse.ContainsKey(track))
                            {
                                mapToUse.Add(track, new List<TrackEvent>());
                            }

                            mapToUse[track].Add(trackEvent);
                        }
                    }
                }
            }
        }

        //Splits track event at a global timecode, keeps the left piece, discards the right piece
        public void SplitAndKeepLeft(ref TrackEvent trackEvent, Timecode globalLocation)
        {
            TrackEvent rightVideoEvent = trackEvent.Split(globalLocation - trackEvent.Start);
            TrackEvent leftVideoEvent = trackEvent;
            if (rightVideoEvent != null)
                trackEvent.Track.Events.Remove(rightVideoEvent);
            trackEvent = leftVideoEvent;
        }

        //Splits track event at a global timecode, keeps the right piece, discards the left piece
        public void SplitAndKeepRight(ref TrackEvent trackEvent, Timecode globalLocation)
        {
            TrackEvent rightVideoEvent = trackEvent.Split(globalLocation - trackEvent.Start);
            TrackEvent leftVideoEvent = trackEvent;
            if (leftVideoEvent != null)
                trackEvent.Track.Events.Remove(leftVideoEvent);
            trackEvent = rightVideoEvent;
        }

        //Trims the start of the track event to a global timecode and sets the duration
        public void TrimEvent(ref TrackEvent trackEvent, Timecode globalLocation, Timecode duration)
        {
            SplitAndKeepRight(ref trackEvent, globalLocation);
            if (trackEvent == null)
                return;
            trackEvent.AdjustStartLength(trackEvent.Start, duration, false);
        }

    }//public class Util

}//namespace EKVegas
