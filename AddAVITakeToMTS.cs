//Finds all VideoEvents in project with a single ActiveTake using .MTS footage,
// then adds a Take with corresponding .AVI footage (without changing active Take).

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
			if(track.IsValid() && track.IsVideo())
			{
				foreach (TrackEvent trackEvent in track.Events)
				{
					if (trackEvent.IsVideo())
					{
						VideoEvent videoEvent = (VideoEvent) trackEvent;
						
						//has single take
						if(videoEvent.Takes.Count == 1)
						{
							string currentMediaPath = videoEvent.ActiveTake.MediaPath;
							string pathExtension = Path.GetExtension(currentMediaPath);
							
							//has find extension
							if(pathExtension.Equals(findExtension, StringComparison.OrdinalIgnoreCase))
							{
								string replacementPath = Path.ChangeExtension(currentMediaPath, replaceExtension);	
								Media media = new Media(replacementPath);
								MediaStream mediaStream = media.GetVideoStreamByIndex(0);
								Timecode oldTakeOffset = videoEvent.ActiveTake.Offset;
								
								//add a new take, without setting as active
								Take addedTake = videoEvent.AddTake(mediaStream, false);								
								addedTake.Offset = oldTakeOffset;
								addedTake.Name += "(AVI)";
								
								//MessageBox.Show("Offset: "+addedTake.Offset);
							}							
						}
					}
				}
				
			}
		}
	}
}