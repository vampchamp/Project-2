using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class recorder : MonoBehaviour
{
    [Header("Live XR Hands")]
    public Transform leftRoot;
    public Transform rightRoot;

    [Header("Tutorial Hands")]
    public Transform tutorialLeftRoot;
    public Transform tutorialRightRoot;
    public GameObject tutorialHands;

    class BoneFrame
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    class Frame
    {
        public List<BoneFrame> left = new();
        public List<BoneFrame> right = new();
    }

    private readonly List<Transform> leftBones = new();
    private readonly List<Transform> rightBones = new();

    private readonly List<Transform> tutorialLeftBones = new();
    private readonly List<Transform> tutorialRightBones = new();

    private readonly List<Frame> recording = new();

    private bool isRecording;
    private bool isPlaying;
    private int playbackFrame;

    void Start()
    {
        GatherBones(leftRoot, leftBones);
        GatherBones(rightRoot, rightBones);

        GatherBones(tutorialLeftRoot, tutorialLeftBones);
        GatherBones(tutorialRightRoot, tutorialRightBones);

        tutorialHands.SetActive(false);

        Debug.Log($"Live Left Bones: {leftBones.Count}");
        Debug.Log($"Tutorial Left Bones: {tutorialLeftBones.Count}");
    }

    void LateUpdate()
    {
        if (isRecording)
            RecordFrame();

        if (isPlaying)
            PlayFrame();
    }
    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
            StartRecording();

        if (Keyboard.current.sKey.wasPressedThisFrame)
            StopRecording();

        if (Keyboard.current.pKey.wasPressedThisFrame)
            StartPlayback();
    }

    void GatherBones(Transform root, List<Transform> list)
    {
        list.Add(root);

        foreach (Transform child in root)
            GatherBones(child, list);
    }

    public void StartRecording()
    {
        Debug.Log("Recording Started");

        recording.Clear();
        playbackFrame = 0;
        isPlaying = false;
        isRecording = true;
    }

    public void StopRecording()
    {
        isRecording = false;

        Debug.Log($"Recording Finished. Frames: {recording.Count}");
    }

    public void StartPlayback()
    {
        if (recording.Count == 0)
        {
            Debug.LogWarning("Nothing has been recorded.");
            return;
        }

        Debug.Log("Playback Started");

        tutorialHands.SetActive(true);

        playbackFrame = 0;
        isPlaying = true;
    }

    void RecordFrame()
    {
        Frame frame = new();

        foreach (Transform bone in leftBones)
        {
            frame.left.Add(new BoneFrame
            {
                position = bone.localPosition,
                rotation = bone.localRotation
            });
        }

        foreach (Transform bone in rightBones)
        {
            frame.right.Add(new BoneFrame
            {
                position = bone.localPosition,
                rotation = bone.localRotation
            });
        }

        recording.Add(frame);
    }

    void PlayFrame()
    {
        if (playbackFrame >= recording.Count)
        {
            Debug.Log("Playback Finished");

            isPlaying = false;
            tutorialHands.SetActive(false);
            return;
        }

        Frame frame = recording[playbackFrame];

        for (int i = 0; i < tutorialLeftBones.Count; i++)
        {
            tutorialLeftBones[i].localPosition = frame.left[i].position;
            tutorialLeftBones[i].localRotation = frame.left[i].rotation;
        }

        for (int i = 0; i < tutorialRightBones.Count; i++)
        {
            tutorialRightBones[i].localPosition = frame.right[i].position;
            tutorialRightBones[i].localRotation = frame.right[i].rotation;
        }

        playbackFrame++;
    }
}