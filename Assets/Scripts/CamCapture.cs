using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/* HOW TO USE
 * Create a new animationt track in the timeline.  Mute all other tracks.
 * In the PREVIEW mode, specify the start and end normalized times in this
 * script's inspector panel.
 * Change the default outpout filename to something appropriate.
 * Check the "Start Capture" checkbox (the checkbox will immediately become
 * unchecked... this is normal).
 */

public class CamCapture : MonoBehaviour
{
    //Public members (shown in the Inspector)

    [Tooltip("Specifies the number of frames to capture.  The larger the number, the smoother the animation.  This number specifies the number of output files that will be generated.")]
    public SR_RenderCamera SRCamera;
    public int NumFrames = 4;
    public float zoom = 1f;
    [Tooltip("If selected, there will be NumFrames+1 generated output files.")]
    public bool IncludeFinalFrame = false;
    [Tooltip("The output file size.  Non-square output files are possible, but only square output files have been tested.")]
    public Vector2Int OutputImageSize = 256 * Vector2Int.one;

    //[Tooltip("The base file name for the animation sequence.")]
    string OutputBaseFileName = "anim";

    [Range(0f, 1f)]
    [Tooltip("Sets the normalized time on the timeline.  This is for visualization and setup only.  Note that for visualization purposes only, the normalized time corresponds to the longest unmuted clip.")]
    public float NormalizedTime = 0f;       //user control during preview
    float m_NormalizedTime = 0f;     //used during capture

    [Tooltip("Set to bebin the capture process (will auto un-set).")]
    public bool StartCapture = false;       //used to start the capture (transition from PREIVEW to START)


    // Non-inspector members

    enum CAPTURE_SM { PREVIEW, TRACK_SELECT, DELAY, START, CAPTURE };    //program state machine
    CAPTURE_SM CaptureSM = CAPTURE_SM.PREVIEW;

    PlayableDirector playableDirector;
    TimelineAsset timelineAsset;
    Camera m_camera;

    float duration;
    int frameNum;
    int StopCount;
    List<bool> TrackMuteStates;
    int trackNum;
    TrackAsset track;
    float StartDelayTime;
    float StartDelay = 0.25f;    //delay 0.25 seconds between tracks

    private void Start()
    {
        playableDirector = GetComponent<PlayableDirector>();
        timelineAsset = playableDirector.playableAsset as TimelineAsset;

        TrackMuteStates = new List<bool>();
        //uncheck inspector Start checkbox
        StartCapture = false;

        //get a reference to the camera
        m_camera = SRCamera.gameObject.GetComponent<Camera>();

        playableDirector.Play();
        
        //Debug.Log("tl.outputTrackCount = " + timelineAsset.outputTrackCount);  // <-- this is the number of tracks in the timeline
        //Debug.Log("playbleGraph.OutputCount = " + playableDirector.playableGraph.GetOutputCount());  // <-- this is the number of unmuted tracks in the timeline

        /*
        foreach (var track in timelineAsset.GetOutputTracks())
        {
            Debug.Log(track.name + "-->" + track.muted + "; duration: " + track.duration);
        }
        */
        //foreach (Component x in GetComponents<Component>()) { Debug.Log(x.GetType()); }
    }


    // Update is called once per frame
    private void Update()
    {




        switch (CaptureSM)      //State Madchine
        {
            case CAPTURE_SM.PREVIEW:        //SM Preview mode

                //do not allow the playableDirector to stop when no track is selected
                if (playableDirector.state == PlayState.Paused) { playableDirector.Play(); }

                duration = (float)playableDirector.duration;    //has to be dynamic (user can change selected track)
                m_camera.orthographicSize = zoom;

                playableDirector.time = NormalizedTime * duration;

                if (StartCapture)
                {
                    StartCapture = false;

                    if (NumFrames < 1)  //check for errors here.  Do not proceed if we find any.
                    {
                        Debug.Log("The value of NumFrames must be > 0.  Current value is " + NumFrames);
                    }
                    else
                    {
                        //add warning messages here... but proceed to next state
                        if (OutputImageSize.x != OutputImageSize.y)
                        {
                            Debug.Log("Warning: Output image is not square...(" + OutputImageSize.x + "X" + OutputImageSize.y + ").  This is not yet fully supported.");
                        }

                        trackNum = 0;

                        StoreTracksMuteStates();

                        //increment the frameNum if the user elects to include the final frame
                        StopCount = NumFrames + (IncludeFinalFrame ? 1 : 0);

                        //set the output image dimensions
                        SRCamera.OutputImageWidth = OutputImageSize.x;
                        SRCamera.OutputImageHeight = OutputImageSize.y;

                        //move to START state
                        CaptureSM = CAPTURE_SM.TRACK_SELECT;
                    }
                }

                break;

            case CAPTURE_SM.TRACK_SELECT:      //SM Start mode (do misc setup and initializations in preparation for capture)

                if (trackNum == timelineAsset.outputTrackCount) //we're done
                {
                    RestoreTracksMuteStates();
                    CaptureSM = CAPTURE_SM.PREVIEW;
                    break;
                }

                if (TrackMuteStates[trackNum])  //skip muted tracks
                {
                    Debug.Log("Skipping track " + trackNum + " (" + timelineAsset.GetOutputTrack(trackNum).name + ")");
                    trackNum++;
                    break; 
                }

                track = SelectTrack(trackNum);  //select the track
                trackNum++;     //increment trackNum

                StartDelayTime = Time.time + StartDelay;
                CaptureSM = CAPTURE_SM.DELAY;

                break;

            case CAPTURE_SM.DELAY:
                if (Time.time < StartDelayTime) break;
                CaptureSM = CAPTURE_SM.START;
                break;

            case CAPTURE_SM.START:
                duration = (float)track.duration;
                frameNum = 0;
                m_NormalizedTime = 0;

                //go to the starting frame within the animation track
                playableDirector.time = 0f;
                playableDirector.Play();

                OutputBaseFileName = track.name;

                //create the output directory based on the provided output base file name
                Directory.CreateDirectory("Output/" + OutputBaseFileName);

                Debug.Log("Processing track " + trackNum + " (" + track.name + ") duration: " + track.duration);

                StartDelayTime = Time.time + StartDelay;

                //transition to the CAPTURE state
                CaptureSM = CAPTURE_SM.CAPTURE;

                break;

            case CAPTURE_SM.CAPTURE:    //SM Capture mode (perform screen captures)
                if (frameNum == StopCount)
                {
                    CaptureSM = CAPTURE_SM.TRACK_SELECT;
                }
                else
                {
                    string filename;
                    filename = "Output/" + OutputBaseFileName + "/" + OutputBaseFileName + frameNum.ToString("000") + ".png";
                    //Debug.Log("File = " + filename + ":  frame = " + m_NormalizedTime + ":  time = " + playableDirector.time);

                    SRCamera.CamCapture(filename);

                    frameNum++;
                    //calculate the normalized time and the corresponding actual time within the animation track
                    m_NormalizedTime = ((float)frameNum) / NumFrames;
                    //playableDirector.time = duration * (CaptureNormStartTime + m_NormalizedTime * (CaptureNormEndTime - CaptureNormStartTime));
                    playableDirector.time = m_NormalizedTime * duration;
                }
                break;
        }

    }

    void StoreTracksMuteStates()
    {
        TrackMuteStates.Clear();

        foreach (var track in timelineAsset.GetOutputTracks())
        {
            TrackMuteStates.Add(track.muted);       //store the current muted state
        }
    }

    void RestoreTracksMuteStates()
    {
        int i = 0;
        foreach (var track in timelineAsset.GetOutputTracks())
        {
            track.muted = TrackMuteStates[i]; //restore the track's muted state
            i++;
        }
    }

    void MuteAllTracks()
    {
        foreach (var track in timelineAsset.GetOutputTracks())
        {
            track.muted = true; //mute the track
        }
    }


/* function to mute all but the specified TL animation track. */
TrackAsset SelectTrack(int trackNum)
    {
        MuteAllTracks();
        TrackAsset track = timelineAsset.GetOutputTrack(trackNum);
        track.muted = false;
        playableDirector.RebuildGraph();
        return track;
    }
}
