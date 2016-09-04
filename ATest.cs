using System;
using Sony.Vegas;

class EntryPoint
{	
	public void FromVegas(Vegas vegas)
	{
		foreach (Track track in vegas.Project.Tracks)
		{
			foreach (TrackEvent trackEvent in track.Events)
			{
				if (trackEvent.IsVideo())
				{
					VideoEvent videoEvent = (VideoEvent) trackEvent;
					videoEvent.ResampleMode = VideoResampleMode.Disable;
					
				}
				else
				{
					trackEvent.Selected = false;
				}
			}
		}
	}
}