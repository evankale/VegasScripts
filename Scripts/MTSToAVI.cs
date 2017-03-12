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

//Finds all VideoEvents in selected VideoTracks with an ActiveTake using .MTS footage,
// then adds a Take with corresponding .AVI footage (and sets it as the active Take).

using System;
using System.IO;
using ScriptPortal.Vegas;

class EntryPoint
{	
	string findExtension = ".mts";
	string replaceExtension = ".avi";

	public void FromVegas(Vegas vegas)
	{
		foreach (Track track in vegas.Project.Tracks)
		{
			if(track.IsValid() && track.IsVideo() && track.Selected)
			{
				foreach (TrackEvent trackEvent in track.Events)
				{
					if (trackEvent.IsVideo())
					{
						VideoEvent videoEvent = (VideoEvent) trackEvent;
						string currentMediaPath = videoEvent.ActiveTake.MediaPath;
						string pathExtension = Path.GetExtension(currentMediaPath);
						
						if(pathExtension.Equals(findExtension, StringComparison.OrdinalIgnoreCase))
						{
							string replacementPath = Path.ChangeExtension(currentMediaPath, replaceExtension);	
							Media media = new Media(replacementPath);
							MediaStream mediaStream = media.GetVideoStreamByIndex(0);
							Timecode oldTakeOffset = videoEvent.ActiveTake.Offset;
							videoEvent.AddTake(mediaStream, true);
							videoEvent.ActiveTake.Offset = oldTakeOffset;
						}
					}
				}
				
			}
		}
	}
}