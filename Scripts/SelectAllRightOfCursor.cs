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

//Selects all TrackEvents right of cursor.

using System;
using System.IO;
using ScriptPortal.Vegas;
using EKVegas;

class EntryPoint
{	
	Vegas vegas = null;
	Util util = null;

	public void FromVegas(Vegas vegas)
	{
		this.vegas = vegas;
		this.util = new Util(vegas);
        
		foreach (Track track in vegas.Project.Tracks)
		{
			if(track.IsValid())
			{
				foreach (TrackEvent trackEvent in track.Events)
				{
					if(util.IsTimecodeBeforeTrackEvent(trackEvent, vegas.Cursor, true))
					{
						trackEvent.Selected = true;
					}
					else
					{
						trackEvent.Selected = false;
					}
				}

			}
		}
	}
}