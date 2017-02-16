//Finds all VideoEvents in project with multiple takes
// then sets their ActiveTake to Takes[0]

using System;
using System.IO;
using System.Windows.Forms;
using Sony.Vegas;

class EntryPoint
{	
	public void FromVegas(Vegas vegas)
	{
		foreach (Track track in vegas.Project.Tracks)
		{
			if(track.IsValid() && track.IsVideo())
			{
				foreach (TrackEvent trackEvent in track.Events)
				{
					if (trackEvent.IsVideo())
					{
						VideoEvent videoEvent = (VideoEvent) trackEvent;
						
						//has multiple takes
						if(videoEvent.Takes.Count > 1)
						{
							//sets ActiveTake to Takes[0]
							videoEvent.ActiveTake = videoEvent.Takes[0];
						}
					}
				}
				
			}
		}
	}
}