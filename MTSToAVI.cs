//Finds all VideoEvents in selected VideoTracks with an ActiveTake using .MTS footage,
// then adds a Take with corresponding .AVI footage (and sets it as the active Take).

using System;
using System.IO;
using System.Windows.Forms;
using Sony.Vegas;

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
							//MessageBox.Show("In: "+timecodeIn+"\n Out: "+timecodeOut);
						}
					}
				}
				
			}
		}
	}
}