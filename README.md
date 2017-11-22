# VegasScripts
- Author: Evan Kale
- Email: evankale91@gmail.com
- Web: www.youtube.com/EvanKale
- Social: @EvanKale91

Various scripts for MAGIX (Sony) Vegas.
- Tested on v14.0 (Build 201)

Compilation note for Sony Vegas (v13 and under):
- The namespace name of the .NET assembly has changed from Sony.Vegas to ScriptPortal.Vegas in v14 and onward
  - Change "using ScriptPortal.Vegas;" to "using Sony.Vegas;" in the scripts to compile for v13 and under


Scripts
=======

AddAVITakeToMTS
- Finds all VideoEvents in project with a single ActiveTake using .MTS footage, then adds a Take with corresponding .AVI footage (without changing active Take).

GroupOverlappingEvents
- Groups overlapping selected track events.
- Requires 1 track to be selected that contains selected track events.

DisableResampleForAllVideos
- Disables video resampling on all video events

MapBlipsToMIDI
- Maps all blips (created by the Blipper) to MIDI notes of an input MIDI file.

MarkBlipsInSelectedVideoEvent
- Finds all blips of a selected video event(inserted by the Blipper) and adds a bookmark at that location.

MTSToAVI
- Finds all VideoEvents in selected VideoTracks with an ActiveTake using .MTS footage, then adds a Take with corresponding .AVI footage (and sets it as the active Take).

SelectAllLeftOfCursor
- Selects all TrackEvents left of cursor.

SelectAllRightOfCursor
- Selects all TrackEvents right of cursor.

SelectRegionEvents
- Selects all track events within the selection region.

SetAllTake0
- Finds all VideoEvents in project with multiple takes then sets their ActiveTake to Takes[0]

SetAllTake1
- Finds all VideoEvents in project with multiple takes then sets their ActiveTake to Takes[1]

SetCursorLeftOfSelection
- Sets the cursor to the left of the leftmost selected TrackEvent

SetCursorRightOfSelection
- Sets the cursor to the left of the rightmost selected TrackEvent

SelectRegionEvents
- Selects all track events within the selection region.

SplitBlips
- Creates splits where blips are made in the left channel audio during the span of the selected video track.